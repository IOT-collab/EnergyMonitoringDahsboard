using IEC.CommonService;
using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Data;

namespace IECGUI.ViewModel
{
    public class EnergyMonitorViewModel2 : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigation;
        private readonly DeviceRuntimeService _deviceRuntime;
        private readonly Dictionary<string, MetersConfig> _meterConfigMap;
        private readonly SafePoller _liveDataTimer;
        private MeterViewModel? _selectedMeter;
        private int _connectedMeterCount;
        private string _meterFilter = string.Empty;
        private string _selectedSection = "All Sections";
        private string _selectedUtilityRoom = "All Utility Rooms";

        public ObservableCollection<MeterViewModel> Meters { get; }
        public ObservableCollection<string> Sections { get; } = new();
        public ObservableCollection<string> UtilityRooms { get; } = new();
        public ObservableCollection<DeviceGroupSummary> GroupSummaries { get; } = new();
        public ICollectionView FilteredMeters { get; }
        public string MeterFilter
        {
            get => _meterFilter;
            set
            {
                if (SetProperty(ref _meterFilter, value))
                    FilteredMeters.Refresh();
            }
        }
        public string SelectedSection
        {
            get => _selectedSection;
            set
            {
                if (SetProperty(ref _selectedSection, value))
                {
                    RefreshUtilityRooms();
                    FilteredMeters.Refresh();
                }
            }
        }
        public string SelectedUtilityRoom
        {
            get => _selectedUtilityRoom;
            set { if (SetProperty(ref _selectedUtilityRoom, value)) FilteredMeters.Refresh(); }
        }

        public MeterViewModel? SelectedMeter
        {
            get => _selectedMeter;
            set => SetProperty(ref _selectedMeter, value);
        }

        public int MeterCount => Meters.Count;
        public int ConnectedMeterCount
        {
            get => _connectedMeterCount;
            private set
            {
                if (SetProperty(ref _connectedMeterCount, value))
                    OnPropertyChanged(nameof(MeterConnectionSummary));
            }
        }

        public string MeterConnectionSummary => $"{ConnectedMeterCount} of {MeterCount} meters connected";

        public ICommand BackCommand { get; }
        public ICommand RelayPage { get; }
        public ICommand MqttBrowserCommand { get; }

        public EnergyMonitorViewModel2(
            INavigationService navigation,
            ConfigurationManagerService config,
            DeviceRuntimeService deviceRuntime)
        {
            _navigation = navigation;
            _deviceRuntime = deviceRuntime;

            var configuredMeters = config.Configuration?.Meters?
                .Where(m => m != null && m.IsEnabled && !string.IsNullOrWhiteSpace(m.MeterName))
                .ToList() ?? new List<MetersConfig>();

            _meterConfigMap = configuredMeters
                .GroupBy(m => m.MeterName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            Meters = new ObservableCollection<MeterViewModel>(
                _meterConfigMap.Keys.Select(name => new MeterViewModel
                {
                    MeterName = name,
                    Section = NormalizeLocation(_meterConfigMap[name].Section, "General"),
                    UtilityRoom = NormalizeLocation(_meterConfigMap[name].UtilityRoom, "Main Utility Room"),
                    MeterStatus = "Connecting"
                }));

            Sections.Add("All Sections");
            foreach (var section in Meters.Select(x => x.Section).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
                Sections.Add(section);
            RefreshUtilityRooms();

            FilteredMeters = CollectionViewSource.GetDefaultView(Meters);
            FilteredMeters.Filter = item =>
            {
                if (item is not MeterViewModel meter) return false;
                if (SelectedSection != "All Sections" && !string.Equals(meter.Section, SelectedSection, StringComparison.OrdinalIgnoreCase)) return false;
                if (SelectedUtilityRoom != "All Utility Rooms" && !string.Equals(meter.UtilityRoom, SelectedUtilityRoom, StringComparison.OrdinalIgnoreCase)) return false;
                if (string.IsNullOrWhiteSpace(MeterFilter)) return true;
                var filter = MeterFilter.Trim();
                return (meter.MeterName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       (meter.MeterStatus?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
                       meter.Section.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                       meter.UtilityRoom.Contains(filter, StringComparison.OrdinalIgnoreCase);
            };

            SelectedMeter = Meters.FirstOrDefault();

            BackCommand = new RelayCommand(NavigateToHome);
            RelayPage = new RelayCommand(() => _navigation.NavigateTo<Iec61850MonitorViewModel>());
            MqttBrowserCommand = new RelayCommand(() => _navigation.NavigateTo<MqttMonitorViewModel>());
            
            _liveDataTimer = new SafePoller(
                TimeSpan.FromMilliseconds(500),
                PollAsync,
                ex => Console.WriteLine($"Gauge monitor polling error: {ex.Message}"));

            _ = InitializeMetersAsync();
        }

        private async Task InitializeMetersAsync()
        {
            if (_meterConfigMap.Count == 0)
                return;

            try
            {
                await _deviceRuntime.StartAsync();
                _liveDataTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gauge meter configuration error: {ex.Message}");
                foreach (var meter in Meters)
                    meter.MeterStatus = "Configuration error";
            }
        }

        private async Task PollAsync(Dictionary<int, object> parameters)
        {
            var readings = _deviceRuntime.GetSnapshot();
            var connected = 0;

            foreach (var meter in Meters)
            {
                MeterReading? reading = null;
                if (!string.IsNullOrWhiteSpace(meter.MeterName))
                    readings.TryGetValue(meter.MeterName, out reading);

                if (string.IsNullOrWhiteSpace(meter.MeterName) ||
                    reading == null || !reading.Values.Any(v => v.Value != null))
                {
                    meter.MeterStatus = string.IsNullOrWhiteSpace(reading?.CommunicationError)
                        ? "No data"
                        : reading.CommunicationError;
                    continue;
                }

                connected++;
                meter.MeterStatus = "Online";

                if (_meterConfigMap.TryGetValue(meter.MeterName, out var meterConfig))
                    ApplyReading(meter, meterConfig, reading);
            }

            ConnectedMeterCount = connected;
            Application.Current?.Dispatcher.BeginInvoke(new Action(RefreshGroupSummaries));
        }

        private void RefreshUtilityRooms()
        {
            UtilityRooms.Clear();
            UtilityRooms.Add("All Utility Rooms");
            var rooms = Meters.Where(x => SelectedSection == "All Sections" ||
                    string.Equals(x.Section, SelectedSection, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.UtilityRoom).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x);
            foreach (var room in rooms) UtilityRooms.Add(room);
            if (!UtilityRooms.Contains(SelectedUtilityRoom)) SelectedUtilityRoom = "All Utility Rooms";
        }

        private void RefreshGroupSummaries()
        {
            GroupSummaries.Clear();
            foreach (var group in Meters.GroupBy(x => new { x.Section, x.UtilityRoom }).OrderBy(x => x.Key.Section).ThenBy(x => x.Key.UtilityRoom))
            {
                var total = group.Count();
                var online = group.Count(x => string.Equals(x.MeterStatus, "Online", StringComparison.OrdinalIgnoreCase));
                GroupSummaries.Add(new DeviceGroupSummary
                {
                    GroupName = $"{group.Key.Section} / {group.Key.UtilityRoom}",
                    MeterSummary = $"{online} of {total} online",
                    OnlinePercent = total == 0 ? 0 : online * 100d / total
                });
            }
        }

        private static string NormalizeLocation(string? value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        private static void ApplyReading(MeterViewModel meter, MetersConfig config, MeterReading reading)
        {
            foreach (var register in config.Registers.Where(r => r.IsEnabled))
            {
                var readingKey = register.ParameterName ?? register.RegisterAddress.ToString();
                if (!reading.Values.TryGetValue(readingKey, out var rawValue) || rawValue == null)
                    continue;

                float value;
                try { value = Convert.ToSingle(rawValue); }
                catch { continue; }

                // Meters commonly return IEEE NaN for calculated quantities such
                // as power factor when there is no load. Gauges and WPF animations
                // require a finite value, so represent that condition as zero.
                if (float.IsNaN(value) || float.IsInfinity(value))
                    value = 0f;

                switch (Normalize(register.ParameterName))
                {
                    case "voltagean": meter.VoltageA_N = value; break;
                    case "voltagebn": meter.VoltageB_N = value; break;
                    case "voltagecn": meter.VoltageC_N = value; break;
                    case "voltagelnavg": meter.VoltageL_N_Avg = value; break;
                    case "currenta": meter.CurrentA = value; break;
                    case "currentb": meter.CurrentB = value; break;
                    case "currentc": meter.CurrentC = value; break;
                    case "currentavg": meter.CurrentAvg = value; break;
                    case "totalactivepower":
                    case "activepower": meter.TotalActivePower = value; break;
                    case "totalreactivepower":
                    case "reactivepower": meter.TotalReactivePower = value; break;
                    case "totalapparentpower":
                    case "apparentpower": meter.TotalApparentPower = value; break;
                    case "frequency": meter.Frequency = value; break;
                    case "powerfactor":
                    case "totalpowerfactor": meter.TotalPowerFactor = value; break;
                }
            }
        }

        private static string Normalize(string? value) =>
            new string((value ?? string.Empty)
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());

        private void NavigateToHome()
        {
            Dispose();
            _navigation.NavigateTo<HomePageViewModel>();
        }

        public void Dispose() => _liveDataTimer.Dispose();
    }

    public class DeviceGroupSummary
    {
        public string GroupName { get; set; } = string.Empty;
        public string MeterSummary { get; set; } = string.Empty;
        public double OnlinePercent { get; set; }
    }
}
