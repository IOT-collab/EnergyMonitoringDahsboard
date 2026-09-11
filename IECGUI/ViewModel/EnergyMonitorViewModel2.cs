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
        private readonly IMqttClientService _mqttService;
        private readonly MqttConfigurationService _mqttConfiguration;
        private readonly Dictionary<string, MetersConfig> _meterConfigMap;
        private readonly HashSet<string> _mqttMeterNames = new(StringComparer.OrdinalIgnoreCase);
        private readonly SafePoller _liveDataTimer;
        private MeterViewModel? _selectedMeter;
        private int _connectedMeterCount;
        private string _meterFilter = string.Empty;
        private string _selectedSection = "All Sections";
        private string _selectedUtilityRoom = "All Utility Rooms";
        private bool _mqttStarted;

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
            DeviceRuntimeService deviceRuntime,
            IMqttClientService mqttService,
            MqttConfigurationService mqttConfiguration)
        {
            _navigation = navigation;
            _deviceRuntime = deviceRuntime;
            _mqttService = mqttService;
            _mqttConfiguration = mqttConfiguration;

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

            _mqttService.OnMessageReceived += OnMqttMessageReceived;
            _ = InitializeMetersAsync();
        }

        private async Task InitializeMetersAsync()
        {
            try
            {
                if (_meterConfigMap.Count > 0)
                    await _deviceRuntime.StartAsync();
                await StartMqttAsync();
                _liveDataTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gauge meter configuration error: {ex.Message}");
                foreach (var meter in Meters.Where(m => !_mqttMeterNames.Contains(m.MeterName)))
                    meter.MeterStatus = "Configuration error";
            }
        }

        private async Task StartMqttAsync()
        {
            var configuration = _mqttConfiguration.Current;
            var subscriptions = configuration.Subscriptions?
                .Where(s => s != null && s.IsEnabled && !string.IsNullOrWhiteSpace(s.Topic))
                .ToList() ?? new List<MqttSubscriptionConfig>();
            if (subscriptions.Count == 0)
                return;

            try
            {
                await _mqttService.ConnectAsync(
                    configuration.Host,
                    configuration.Port,
                    configuration.Username,
                    configuration.Password,
                    configuration.UseTls,
                    configuration.CertificatePath,
                    configuration.AllowUntrustedCertificates,
                    configuration.ClientId,
                    configuration.KeepAliveSeconds,
                    configuration.AutoReconnect);

                foreach (var subscription in subscriptions)
                    await _mqttService.SubscribeAsync(subscription.Topic);

                _mqttStarted = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gauge MQTT connection error: {ex.Message}");
                _mqttStarted = false;
            }
        }

        private async Task PollAsync(Dictionary<int, object> parameters)
        {
            var readings = _deviceRuntime.GetSnapshot();
            var connected = 0;

            foreach (var meter in Meters)
            {
                if (_mqttMeterNames.Contains(meter.MeterName))
                {
                    if (string.Equals(meter.MeterStatus, "Online", StringComparison.OrdinalIgnoreCase))
                        connected++;
                    continue;
                }

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

        private void OnMqttMessageReceived(object? sender, MqttMessageReceivedArgs message)
        {
            RunOnUi(() => ProcessMqttMessage(message));
        }

        private void ProcessMqttMessage(MqttMessageReceivedArgs message)
        {
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(message.Payload);
                if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
                    return;

                var meter = GetOrCreateMqttMeter(message.Topic);
                var values = ExtractMqttValues(document.RootElement, message.Topic);
                foreach (var value in values)
                {
                    // Keep the configured display name for the parameter list,
                    // while using the JSON path as the gauge alias. This means
                    // users can rename a field (for example, "Main Current")
                    // without losing the automatic Current/Voltage/PF mapping.
                    ApplyMqttValue(meter, value.DisplayName, value.SourcePath,
                        value.RawValue, FindMqttUnit(value.SourcePath));
                }

                meter.MeterStatus = "Online";
                ConnectedMeterCount = Meters.Count(x => string.Equals(x.MeterStatus, "Online", StringComparison.OrdinalIgnoreCase));
                RefreshGroupSummaries();
                FilteredMeters.Refresh();
            }
            catch (System.Text.Json.JsonException)
            {
                // Non-JSON MQTT messages remain available in the MQTT monitor,
                // but cannot be represented by the numeric gauges.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gauge MQTT message error: {ex.Message}");
            }
        }

        private MeterViewModel GetOrCreateMqttMeter(string topic)
        {
            var existingName = _mqttMeterNames.FirstOrDefault(name =>
                string.Equals(name, MqttMeterName(topic), StringComparison.OrdinalIgnoreCase));
            if (existingName != null)
                return Meters.First(m => string.Equals(m.MeterName, existingName, StringComparison.OrdinalIgnoreCase));

            var meter = new MeterViewModel
            {
                MeterName = MqttMeterName(topic),
                Section = "MQTT",
                UtilityRoom = string.IsNullOrWhiteSpace(_mqttConfiguration.Current.DisplayName)
                    ? "MQTT Broker"
                    : _mqttConfiguration.Current.DisplayName,
                MeterStatus = "Waiting"
            };
            _mqttMeterNames.Add(meter.MeterName);
            Meters.Add(meter);
            if (!Sections.Contains(meter.Section)) Sections.Add(meter.Section);
            RefreshUtilityRooms();
            OnPropertyChanged(nameof(MeterCount));
            if (SelectedMeter == null)
                SelectedMeter = meter;
            return meter;
        }

        private string MqttMeterName(string topic)
        {
            var displayName = _mqttConfiguration.Current.DisplayName;
            var suffix = topic.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (string.IsNullOrWhiteSpace(suffix)) suffix = "Meter";
            return string.IsNullOrWhiteSpace(displayName) ? $"MQTT / {suffix}" : $"{displayName} / {suffix}";
        }

        private List<(string DisplayName, string SourcePath, string RawValue)> ExtractMqttValues(
            System.Text.Json.JsonElement root, string topic)
        {
            var values = new List<(string DisplayName, string SourcePath, string RawValue)>();
            var mappings = _mqttConfiguration.Current.FieldMappings ?? new List<MqttFieldMapping>();
            foreach (var mapping in mappings.Where(m => m != null && m.IsEnabled &&
                         (string.IsNullOrWhiteSpace(m.TopicFilter) || TopicMatches(m.TopicFilter, topic))))
            {
                if (TryGetJsonValue(root, mapping.JsonPath, out var value))
                    values.Add((MappingName(mapping), mapping.JsonPath, value));
            }

            // If no mappings have been configured yet, use every top-level field.
            if (values.Count == 0)
            {
                foreach (var property in root.EnumerateObject())
                    values.Add((property.Name, property.Name, property.Value.ToString()));
            }
            return values;
        }

        private static string FindMqttUnit(string key)
        {
            var normalized = Normalize(key);
            if (normalized.Contains("voltage")) return "V";
            if (normalized.Contains("current")) return "A";
            if (normalized.Contains("frequency")) return "Hz";
            if (normalized.Contains("powerfactor") || normalized.Contains("pf")) return "PF";
            if (normalized.Contains("energykwh")) return "kWh";
            if (normalized.Contains("power")) return "kW";
            return string.Empty;
        }

        private static void ApplyMqttValue(
            MeterViewModel meter,
            string displayName,
            string sourcePath,
            string rawValue,
            string unit)
        {
            if (!double.TryParse(rawValue, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var parsed) ||
                double.IsNaN(parsed) || double.IsInfinity(parsed))
                return;

            var value = (float)parsed;
            var parameterName = string.IsNullOrWhiteSpace(displayName) ? sourcePath : displayName;
            var parameter = meter.Parameters.FirstOrDefault(p =>
                string.Equals(p.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase));
            if (parameter == null)
            {
                parameter = new MeterParameterViewModel { ParameterName = parameterName, Unit = unit };
                meter.Parameters.Add(parameter);
            }
            parameter.Value = parsed;

            switch (Normalize(sourcePath))
            {
                case "frequency": meter.Frequency = value; break;
                case "currentiaverage":
                case "currentaverage":
                case "currentavg":
                case "currenta": meter.CurrentAvg = value; break;
                case "voltagevphasenutral":
                case "voltagelnavg":
                case "voltageavg": meter.VoltageL_N_Avg = value; break;
                case "powerptot":
                case "totalactivepower":
                case "activepower": meter.TotalActivePower = value; break;
                case "powerqtot":
                case "totalreactivepower":
                case "reactivepower": meter.TotalReactivePower = value; break;
                case "powerstot":
                case "totalapparentpower":
                case "apparentpower": meter.TotalApparentPower = value; break;
                case "pfpfsystem":
                case "powerfactor":
                case "totalpowerfactor": meter.TotalPowerFactor = Math.Clamp(value, 0f, 1f); break;
            }
        }

        private static bool TryGetJsonValue(System.Text.Json.JsonElement root, string path, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(path)) return false;
            var current = root;
            foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (current.ValueKind != System.Text.Json.JsonValueKind.Object) return false;
                var found = false;
                foreach (var property in current.EnumerateObject())
                {
                    if (!string.Equals(property.Name, segment, StringComparison.OrdinalIgnoreCase)) continue;
                    current = property.Value;
                    found = true;
                    break;
                }
                if (!found) return false;
            }
            value = current.ToString();
            return true;
        }

        private static bool TopicMatches(string filter, string topic)
        {
            var filterParts = filter.Split('/');
            var topicParts = topic.Split('/');
            for (var i = 0; i < filterParts.Length; i++)
            {
                if (filterParts[i] == "#") return true;
                if (i >= topicParts.Length) return false;
                if (filterParts[i] != "+" && !string.Equals(filterParts[i], topicParts[i], StringComparison.OrdinalIgnoreCase)) return false;
            }
            return filterParts.Length == topicParts.Length;
        }

        private static string MappingName(MqttFieldMapping mapping) =>
            string.IsNullOrWhiteSpace(mapping.DisplayName) ? mapping.JsonPath : mapping.DisplayName;

        private static void RunOnUi(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess()) action();
            else dispatcher.BeginInvoke(action);
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
                var readingKey = string.IsNullOrWhiteSpace(register.ParameterName) ? register.RegisterAddress.ToString() : register.ParameterName.Trim();
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

        public void Dispose()
        {
            _mqttService.OnMessageReceived -= OnMqttMessageReceived;
            _liveDataTimer.Dispose();

            // Gauge and MQTT Monitor share the singleton client. Disconnecting
            // here prevents a closed Gauge page from continuing to receive data;
            // the MQTT Monitor reconnects when it is opened again.
            if (_mqttStarted)
                _ = _mqttService.DisconnectAsync();
            _mqttStarted = false;
        }
    }

    public class DeviceGroupSummary
    {
        public string GroupName { get; set; } = string.Empty;
        public string MeterSummary { get; set; } = string.Empty;
        public double OnlinePercent { get; set; }
    }
}
