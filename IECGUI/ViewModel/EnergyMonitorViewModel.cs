using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.Services;
using IPCSoftware.Common.CommonExtensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace IECGUI.ViewModel
{
    public class EnergyMonitorViewModel : BaseViewModel
    {
        public ICommand ReturnToHome { get; }
        public ICommand Connect { get; set; }
        public ICommand Disconnect { get; set; }

        private readonly SafePoller _liveDataTimer;

        private readonly IMultiEnergyMeterService _multiEnergyMeterService;

        private readonly ConfigurationManagerService _config;

        // UI expects per-meter properties (VoltageA_N etc.) so expose MeterViewModel collection
        public ObservableCollection<MeterViewModel> Meters { get; }

        // Keep a map of the saved configuration for each meter (to access registers & comm settings)
        private readonly Dictionary<string, MetersConfig> _meterConfigMap = new();

        private readonly INavigationService _navigation;

        private CancellationTokenSource _cts;

        public EnergyMonitorViewModel(INavigationService navigation, ConfigurationManagerService config, IMultiEnergyMeterService multiEnergyMeterService)
        {
            _liveDataTimer = new SafePoller(TimeSpan.FromMilliseconds(500), RunBackgroundService, ex => Console.WriteLine(ex.Message));
            _liveDataTimer.Start();

            _multiEnergyMeterService = multiEnergyMeterService;
            _navigation = navigation;

            _config = config;

            // Build UI collection from saved configuration
            var configMeters = _config.Configuration?.Meters ?? new List<MetersConfig>();

            // Populate map and create MeterViewModel items
            foreach (var cfg in configMeters)
            {
                if (string.IsNullOrWhiteSpace(cfg?.MeterName))
                    continue;

                _meterConfigMap[cfg.MeterName] = cfg;
            }

            Meters = new ObservableCollection<MeterViewModel>(
                _meterConfigMap.Keys.Select(n => new MeterViewModel { MeterName = n }));

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
                vm.MeterStatus = "Disconnected";
            }   
            Connect = new RelayCommand(async () => await ConnectMeters());
            Disconnect = new RelayCommand(async () => await DisconnectMeters());
            ReturnToHome = new RelayCommand(NavigateToHome);
        }

        // Optional explicit connect method if you want to re-configure/reopen ports at runtime
        public async Task ConnectMeters()
        {
            try
            {
               await _multiEnergyMeterService.Configure(_meterConfigMap.Values);
               foreach (var vm in Meters)
               {
                   vm.MeterStatus = "Connected";
               }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ConnectMeters error: {ex.Message}");
                MessageBox.Show($"Unable to Connect {ex.Message}");
                foreach (var vm in Meters)
                {
                    vm.MeterStatus = "Disconnected";
                }
                return;
            }
        }

        public async Task MultiMeterRuntime()
        {
            try
            {
                // ReadAllAsync expected to return Dictionary<string, MeterReading>
                var readings = await _multiEnergyMeterService.ReadAllAsync();

                foreach (var vm in Meters)
                {
                    if (string.IsNullOrWhiteSpace(vm?.MeterName))
                        continue;

                    if (!readings.TryGetValue(vm.MeterName, out var reading) || reading == null)
                    {
                        Console.WriteLine($"No data for {vm.MeterName}");
                        vm.MeterStatus = $"No data for {vm.MeterName}";
                        continue;
                    }

                    // Find corresponding config and its registers to map parameter names
                    if (!_meterConfigMap.TryGetValue(vm.MeterName, out var cfg) || cfg.Registers == null)
                        continue;

                    foreach (var reg in cfg.Registers)
                    {
                        if (!reg.IsEnabled)
                            continue;

                        var key = reg.ParameterName ?? reg.RegisterAddress.ToString();

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MultiMeterRuntime error: {ex.Message}");
                MessageBox.Show($"Unexpected runtime error: {ex.Message}");
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
            _navigation.NavigateTo<HomePageViewModel>();
        }

        public async Task DisconnectMeters()
        {
            try
            {
               await  _multiEnergyMeterService.DisconnectAll();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DisconnectMeters error: {ex.Message}");
                MessageBox.Show($"Unable to Disconnect {ex.Message}");
                foreach (var vm in Meters)
                {
                    vm.MeterStatus = $"Unable to Disconnect {ex.Message}";
                }
                return;
            }
        }
    }
}
