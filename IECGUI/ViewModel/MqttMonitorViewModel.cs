using IEC.Shared.Services;
using IEC.Shared.Models;
using IECGUI.Services;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace IECGUI.ViewModel
{
    public class MqttMonitorViewModel : BaseViewModel
    {
        private readonly IMqttClientService _mqttService;
        private readonly INavigationService _navigation;

        // ── Connection ─────────────────────────────────
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set { _isConnected = value; OnPropertyChanged(); }
        }

        private string _statusMessage = "Disconnected";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // ── Live Meter Readings (one per remote meter) ─
        public ObservableCollection<MqttMeterReading> LiveReadings { get; }
            = new ObservableCollection<MqttMeterReading>();

        // ── Raw Message Log (for debugging) ────────────
        public ObservableCollection<string> MessageLog { get; }
            = new ObservableCollection<string>();

        // ── Commands ───────────────────────────────────
        public ICommand BackCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }

        public MqttMonitorViewModel(
            INavigationService navigation,
            IMqttClientService mqttService)
        {
            _navigation = navigation;
            _mqttService = mqttService;

            BackCommand = new RelayCommand(() =>
                _navigation.NavigateTo<HomePageViewModel>());

            ConnectCommand = new RelayCommand(async () => await ConnectAsync());
            DisconnectCommand = new RelayCommand(async () => await DisconnectAsync());

            // Subscribe to incoming messages
            _mqttService.OnMessageReceived += OnMessageReceived;

            // Auto connect on startup
            _ = ConnectAsync();
        }

        private async Task ConnectAsync()
        {
            try
            {
                StatusMessage = "Connecting...";
                await _mqttService.ConnectAsync("test.mosquitto.org", 1883);

                // Subscribe to all meters — wildcard '#' or specific topic
                await _mqttService.SubscribeAsync("testtopic/#");

                IsConnected = true;
                StatusMessage = "Connected — Listening for data...";
            }
            catch (Exception ex)
            {
                IsConnected = false;
                StatusMessage = $"Connection Failed: {ex.Message}";
            }
        }

        private async Task DisconnectAsync()
        {
            await _mqttService.DisconnectAsync();
            IsConnected = false;
            StatusMessage = "Disconnected";
        }

        private void OnMessageReceived(object sender, MqttMessageReceivedArgs e)
        {
            try
            {
                
                var parts = e.Topic.Split('/');
                if (parts.Length <1) return;

                string stationId = parts[1];
                string meterId = parts[2];

     
                var reading = JsonSerializer.Deserialize<MqttMeterReading>(
                    e.Payload,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (reading == null) return;

                reading.StationId = stationId;
                reading.MeterId = meterId;
                reading.Timestamp = e.ReceivedAt;

               
                Application.Current.Dispatcher.Invoke(() =>
                {
                    
                    var existing = LiveReadings.FirstOrDefault(
                        r => r.StationId == stationId && r.MeterId == meterId);

                    if (existing != null)
                    {
                        int index = LiveReadings.IndexOf(existing);
                        LiveReadings[index] = reading;
                    }
                    else
                    {
                        LiveReadings.Add(reading);
                    }

                    // Message log (last 50)
                    if (MessageLog.Count > 50)
                        MessageLog.RemoveAt(0);

                    MessageLog.Add($"[{e.ReceivedAt:HH:mm:ss}] {e.Topic} → {e.Payload}");
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    MessageLog.Add($"[ERROR] {ex.Message}"));
            }
        }

        public void Cleanup()
        {
            _mqttService.OnMessageReceived -= OnMessageReceived;
            _ = _mqttService.DisconnectAsync();
        }
    }
}