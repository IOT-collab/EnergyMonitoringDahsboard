using IEC.CommonService;
using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IECGUI.ViewModel
{
    public class EnergyMonitorViewModel2 : BaseViewModel, IDisposable
    {
        private readonly INavigationService _navigation;
        private readonly IMultiEnergyMeterService _meterService;
        private readonly Dictionary<string, MetersConfig> _meterConfigMap;
        private readonly SafePoller _liveDataTimer;
        private MeterViewModel? _selectedMeter;
        private int _connectedMeterCount;

        public ObservableCollection<MeterViewModel> Meters { get; }

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
            IMultiEnergyMeterService meterService)
        {
            _navigation = navigation;
            _meterService = meterService;

            var configuredMeters = config.Configuration?.Meters?
                .Where(m => m != null && !string.IsNullOrWhiteSpace(m.MeterName))
                .ToList() ?? new List<MetersConfig>();

            _meterConfigMap = configuredMeters
                .GroupBy(m => m.MeterName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            Meters = new ObservableCollection<MeterViewModel>(
                _meterConfigMap.Keys.Select(name => new MeterViewModel
                {
                    MeterName = name,
                    MeterStatus = "Connecting"
                }));

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
                await _meterService.Configure(_meterConfigMap.Values);
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
            var readings = await _meterService.ReadAllAsync();
            var connected = 0;

            foreach (var meter in Meters)
            {
                if (string.IsNullOrWhiteSpace(meter.MeterName) ||
                    !readings.TryGetValue(meter.MeterName, out var reading) ||
                    reading == null)
                {
                    meter.MeterStatus = "No data";
                    continue;
                }

                connected++;
                meter.MeterStatus = "Online";

                if (_meterConfigMap.TryGetValue(meter.MeterName, out var meterConfig))
                    ApplyReading(meter, meterConfig, reading);
            }

            ConnectedMeterCount = connected;
        }

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
}
