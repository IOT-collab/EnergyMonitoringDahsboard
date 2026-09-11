using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.Services;
using IEC.CommonService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using IEC.Shared;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using IEC.Shared.Services.Logging;
using System.ComponentModel;
using System.Windows.Data;

namespace IECGUI.ViewModel
{
    public class EnergyMonitorViewModel : BaseViewModel, IDisposable
    {
        public ICommand ReturnToHome { get; }
        private readonly IDialogService _dialogService;

        private readonly SafePoller _liveDataTimer;

        private readonly DeviceRuntimeService _deviceRuntime;

        private EnergyLoggingService? _energyLogger;

        private readonly ConfigurationManagerService _config;

        // UI expects per-meter properties (VoltageA_N etc.) so expose MeterViewModel collection
        public ObservableCollection<MeterViewModel> Meters { get; }
        public ICollectionView FilteredMeters { get; }
        public ObservableCollection<string> Sections { get; } = new();
        public ObservableCollection<string> UtilityRooms { get; } = new();
        public ObservableCollection<DeviceRuntimeMessage> RuntimeMessages { get; } = new();
        private string _selectedSection = "All Sections";
        private string _selectedUtilityRoom = "All Utility Rooms";
        public string SelectedSection
        {
            get => _selectedSection;
            set { if (SetProperty(ref _selectedSection, value)) { RefreshUtilityRooms(); FilteredMeters.Refresh(); } }
        }
        public string SelectedUtilityRoom
        {
            get => _selectedUtilityRoom;
            set { if (SetProperty(ref _selectedUtilityRoom, value)) FilteredMeters.Refresh(); }
        }

        // Keep a map of the saved configuration for each meter (to access registers & comm settings)
        private readonly Dictionary<string, MetersConfig> _meterConfigMap = new();

        private readonly INavigationService _navigation;

        private CancellationTokenSource _cts;

        public EnergyMonitorViewModel(INavigationService navigation, ConfigurationManagerService config, DeviceRuntimeService deviceRuntime, IDialogService dialogService, IAuthService authService)
        {
            _deviceRuntime = deviceRuntime;
            _dialogService = dialogService;
            _navigation = navigation;

            _energyLogger = new EnergyLoggingService(AppPaths.Data, authService.CurrentUser?.Username);


            _config = config;

            _liveDataTimer = new SafePoller(TimeSpan.FromMilliseconds(500), RunBackgroundService, ex => Console.WriteLine(ex.Message));

            // Build UI collection from saved configuration
            var configMeters = (_config.Configuration?.Meters ?? new List<MetersConfig>())
                .Where(m => m != null && m.IsEnabled)
                .ToList();

            // Populate map and create MeterViewModel items
            foreach (var cfg in configMeters)
            {
                if (string.IsNullOrWhiteSpace(cfg?.MeterName))
                    continue;

                _meterConfigMap[cfg.MeterName] = cfg;
            }

            Meters = new ObservableCollection<MeterViewModel>(
                _meterConfigMap.Values.Select(CreateMeterViewModel));

            Sections.Add("All Sections");
            foreach (var section in Meters.Select(x => x.Section).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x)) Sections.Add(section);
            RefreshUtilityRooms();
            FilteredMeters = CollectionViewSource.GetDefaultView(Meters);
            FilteredMeters.Filter = item => item is MeterViewModel meter &&
                (SelectedSection == "All Sections" || string.Equals(meter.Section, SelectedSection, StringComparison.OrdinalIgnoreCase)) &&
                (SelectedUtilityRoom == "All Utility Rooms" || string.Equals(meter.UtilityRoom, SelectedUtilityRoom, StringComparison.OrdinalIgnoreCase));

            // Configure the multi-meter service with the saved MetersConfig list
            //try
            //{
            //    _multiEnergyMeterService.Configure(_meterConfigMap.Values);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Configure multi-meter service failed: {ex.Message}");
            //}
            foreach (var vm in Meters)
            {
                vm.MeterStatus = "Connecting...";
            }   
            ReturnToHome = new RelayCommand(NavigateToHome);
            _liveDataTimer.Start();
            _ = EnsureRuntimeAsync();
        }

        private async Task EnsureRuntimeAsync()
        {
            try
            {
                await _deviceRuntime.StartAsync();
                var meterNames = Meters.Select(m => m.MeterName).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                _energyLogger?.Start(meterNames);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Energy Monitor runtime unavailable: {ex.Message}");
                foreach (var vm in Meters) vm.MeterStatus = "Runtime unavailable";
            }
        }

        public async Task MultiMeterRuntime()
        {
            try
            {
                // ReadAllAsync expected to return Dictionary<string, MeterReading>
                var readings = _deviceRuntime.GetSnapshot();

                foreach (var vm in Meters)
                {
                    if (string.IsNullOrWhiteSpace(vm?.MeterName))
                        continue;

                    if (!readings.TryGetValue(vm.MeterName, out var reading) || reading == null ||
                        !reading.Values.Any(v => v.Value != null))
                    {
                        Console.WriteLine($"No data for {vm.MeterName}");
                        vm.MeterStatus = string.IsNullOrWhiteSpace(reading?.CommunicationError)
                            ? "No response"
                            : reading.CommunicationError;
                        continue;
                    }

                    vm.MeterStatus = "Online";

                    // Find corresponding config and its registers to map parameter names
                    if (!_meterConfigMap.TryGetValue(vm.MeterName, out var cfg) || cfg.Registers == null)
                        continue;

                    foreach (var reg in cfg.Registers)
                    {
                        if (!reg.IsEnabled)
                            continue;

                        var key = string.IsNullOrWhiteSpace(reg.ParameterName)
                            ? reg.RegisterAddress.ToString()
                            : reg.ParameterName.Trim();

                        if (!reading.Values.TryGetValue(key, out var rawValue) || rawValue == null)
                            continue;

                        // Try convert to float safely
                        float valueFloat;
                        try
                        {
                            // rawValue may be double/int/float => convert
                            valueFloat = Convert.ToSingle(rawValue);
                        }
                        catch
                        {
                            continue;
                        }

                        // Update the register-driven card row regardless of whether
                        // this is one of the application's legacy standard fields.
                        var parameter = vm.Parameters.FirstOrDefault(p =>
                            p.RegisterAddress == reg.RegisterAddress);
                        if (parameter != null)
                            parameter.Value = valueFloat;

                        // Map parameter name to MeterViewModel property
                        // Normalize parameter name for comparisons
                        var normalized = (reg.ParameterName ?? string.Empty)
                            .Replace(" ", "")
                            .Replace("-", "")
                            .Replace("_", "")
                            .ToLowerInvariant();

                        switch (normalized)
                        {
                            case "voltagean":
                            case "voltagea-n":
                                vm.VoltageA_N = valueFloat;
                                break;

                            case "voltagebn":
                            case "voltageb-n":
                                vm.VoltageB_N = valueFloat;
                                break;

                            case "voltagecn":
                            case "voltagec-n":
                                vm.VoltageC_N = valueFloat;
                                break;

                            case "voltagelnavg":
                            case "voltageln_avg":
                            case "voltagel-navg":
                                vm.VoltageL_N_Avg = valueFloat;
                                break;

                            case "currenta":
                                vm.CurrentA = valueFloat;
                                break;

                            case "currentb":
                                vm.CurrentB = valueFloat;
                                break;

                            case "currentc":
                                vm.CurrentC = valueFloat;
                                break;

                            case "currentavg":
                                vm.CurrentAvg = valueFloat;
                                break;

                            case "totalactivepower":
                            case "activepower":
                                vm.TotalActivePower = valueFloat;
                                break;

                            case "totalreactivepower":
                            case "reactivepower":
                                vm.TotalReactivePower = valueFloat;
                                break;

                            case "totalapparentpower":
                            case "apparentpower":
                                vm.TotalApparentPower = valueFloat;
                                break;

                            case "frequency":
                                vm.Frequency = valueFloat;
                                break;

                            case "powerfactor":
                            case "totalpowerfactor":
                                vm.TotalPowerFactor = valueFloat;
                                break;

                            default:
                                // Unknown parameter: ignored for now. Could be stored to a per-meter dictionary for diagnostics.
                                break;
                        }
                    }
                }


                var loggerReadings = new Dictionary<string, IDictionary<string, object>>();
                foreach (var vm in Meters)
                {
                    if (string.IsNullOrWhiteSpace(vm.MeterName) ||
                        !readings.TryGetValue(vm.MeterName, out var meterReading) ||
                        meterReading == null)
                        continue;

                    var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    if (_meterConfigMap.TryGetValue(vm.MeterName, out var meterConfig))
                    {
                        foreach (var register in meterConfig.Registers.Where(r => r.IsEnabled))
                        {
                            var parameterName = string.IsNullOrWhiteSpace(register.ParameterName)
                                ? $"Register {register.RegisterAddress}"
                                : register.ParameterName.Trim();
                            var readingKey = string.IsNullOrWhiteSpace(register.ParameterName) ? register.RegisterAddress.ToString() : register.ParameterName.Trim();
                            if (meterReading.Values.TryGetValue(readingKey, out var value) && value != null)
                                values[parameterName] = value;
                        }
                    }

                    if (values.Count > 0)
                        loggerReadings[vm.MeterName] = values;
                }

                var connected = readings.Values.Count(x => x != null && x.Values.Any(v => v.Value != null));
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    RuntimeMessages.Clear();
                    RuntimeMessages.Add(new DeviceRuntimeMessage
                    {
                        Timestamp = DateTime.Now,
                        Source = "Device Runtime",
                        Message = $"{connected} of {Meters.Count} enabled devices responding. Central status: {_deviceRuntime.Status}"
                    });
                }));
                _energyLogger?.AppendReadings(loggerReadings);


            }
            catch (Exception ex)
            {
                Console.WriteLine($"MultiMeterRuntime error: {ex.Message}");
                foreach (var vm in Meters)
                {
                    vm.MeterStatus = $"Unexpected runtime error: {ex.Message}";
                }
                return;
            }
        }

        public async Task RunBackgroundService(Dictionary<int, object> parameters)
        {
            await MultiMeterRuntime();
        }

        private void NavigateToHome()
        {
            _liveDataTimer.Stop();
            _energyLogger?.Stop();
            _navigation.NavigateTo<HomePageViewModel>();
        }

        private static MeterViewModel CreateMeterViewModel(MetersConfig config)
        {
            var meter = new MeterViewModel
            {
                MeterName = config.MeterName,
                Section = string.IsNullOrWhiteSpace(config.Section) ? "General" : config.Section.Trim(),
                UtilityRoom = string.IsNullOrWhiteSpace(config.UtilityRoom) ? "Main Utility Room" : config.UtilityRoom.Trim()
            };

            foreach (var register in config.Registers.Where(r => r.IsEnabled))
            {
                meter.Parameters.Add(new MeterParameterViewModel
                {
                    ParameterName = string.IsNullOrWhiteSpace(register.ParameterName)
                        ? $"Register {register.RegisterAddress}"
                        : register.ParameterName,
                    RegisterAddress = register.RegisterAddress,
                    Unit = register.Unit ?? string.Empty
                });
            }

            return meter;
        }

        private void RefreshUtilityRooms()
        {
            UtilityRooms.Clear();
            UtilityRooms.Add("All Utility Rooms");
            foreach (var room in Meters.Where(x => SelectedSection == "All Sections" || string.Equals(x.Section, SelectedSection, StringComparison.OrdinalIgnoreCase))
                         .Select(x => x.UtilityRoom).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
                UtilityRooms.Add(room);
            if (!UtilityRooms.Contains(SelectedUtilityRoom)) SelectedUtilityRoom = "All Utility Rooms";
        }

        public void Dispose()
        {
            _liveDataTimer.Dispose();
            _energyLogger?.Stop();
        }
    }

    public class DeviceRuntimeMessage
    {
        public DateTime Timestamp { get; set; }
        public string TimestampText => Timestamp.ToString("dd-MMM-yyyy HH:mm:ss");
        public string Source { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
