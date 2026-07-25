using IEC.Shared.IECInterface;
using IEC.Shared.IECModels;
using IEC.Shared.IECServices;
using IECGUI.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace IECGUI.ViewModel
{
    public class Iec61850MonitorViewModel : BaseViewModel
    {
        private readonly IIec61850MeterService _service;
        private readonly INavigationService _navigation;
        private readonly IecConfigManagerService _configManager;

        private DispatcherTimer _pollTimer;

        // ── Connection Status ──────────────────────────────────
        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            set { _isOnline = value; OnPropertyChanged(); }
        }

        private string _statusMessage = "Disconnected";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // ── Multi Relay Readings (DataGrid source) ─────────────
        public ObservableCollection<RelayReadingModel> RelayReadings { get; }
            = new ObservableCollection<RelayReadingModel>();

        // ── Selected Relay (Detail Panel) ──────────────────────
        private RelayReadingModel _selectedRelay;
        public RelayReadingModel SelectedRelay
        {
            get => _selectedRelay;
            set { _selectedRelay = value; OnPropertyChanged(); }
        }

        // ── Commands ───────────────────────────────────────────
        public ICommand BackCommand { get; }
        public ICommand ConfigurationCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }


        public Iec61850MonitorViewModel(
            INavigationService navigation,
            IIec61850MeterService service,
            IecConfigManagerService configManager)
        {
            _navigation = navigation;
            _service = service;
            _configManager = configManager;

            BackCommand = new RelayCommand(() =>
                _navigation.NavigateTo<HomePageViewModel>());

            ConfigurationCommand = new RelayCommand(() =>
                _navigation.NavigateTo<IecConfigViewModel>());


            ConnectCommand = new RelayCommand(async () => await ConnectAsync());
            DisconnectCommand = new RelayCommand(async () => await DisconnectAsync());

           
        }
        private async Task ConnectAsync()
        {
            if (!IsOnline)
            {
                await InitializeAsync();
            }
        }

        private async Task DisconnectAsync()
        {
            Cleanup();
            IsOnline = false;
            StatusMessage = "Disconnected";
            RelayReadings.Clear();
        }
        private async Task InitializeAsync()
        {
            try
            {
                var config = _configManager.Load();

                if (!config.Relays.Any())
                {
                    config.Relays.Add(IecDefaultConfig.GetDefault());
                    _configManager.Save(config);
                }

                StatusMessage = "Connecting to relays...";

                await _service.ConnectAllAsync(config.Relays);

                // Pre-populate rows so UI shows relay names immediately
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var relay in config.Relays.Where(r => r.IsEnabled))
                    {
                        RelayReadings.Add(new RelayReadingModel
                        {
                            RelayId = relay.RelayId,
                            RelayName = relay.RelayName,
                            IsOnline = false
                        });
                    }

                    SelectedRelay = RelayReadings.FirstOrDefault();
                });

                IsOnline = true;
                StatusMessage = $"Connected — {config.Relays.Count(r => r.IsEnabled)} relay(s)";

                _pollTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(
                        config.Relays.Any() ? config.Relays.Min(r => r.PollIntervalMs) : 3000)
                };
                _pollTimer.Tick += async (s, e) => await PollAsync();
                _pollTimer.Start();
            }
            catch (Exception ex)
            {
                IsOnline = false;
                StatusMessage = $"Init Failed: {ex.Message}";
            }
        }

        private async Task PollAsync()
        {
            try
            {
                var results = await _service.ReadAllAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var result in results)
                    {
                        var existing = RelayReadings
                            .FirstOrDefault(r => r.RelayId == result.Key);

                        if (existing != null)
                        {
                            // Update IN-PLACE — object reference stays same
                            // DataGrid row stays selected, detail panel stays visible
                            existing.UpdateFrom(result.Value);
                        }
                    }

                    IsOnline = RelayReadings.Any(r => r.IsOnline);
                    StatusMessage = IsOnline ? "Live" : "Connection Lost";
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsOnline = false;
                    StatusMessage = $"Poll Error: {ex.Message}";
                });
            }
        }

        public void Cleanup()
        {
            _pollTimer?.Stop();
            _service?.Disconnect();
        }
    }
}