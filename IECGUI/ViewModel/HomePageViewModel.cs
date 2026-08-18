using IEC.Shared.Services;
using IECGUI.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace IECGUI.ViewModel
{
    public class HomePageViewModel : BaseViewModel
    {
        public ICommand SldViewCommand { get; }
        public ICommand EnergyViewCommand { get; }
        public ICommand GaugeViewCommand { get; }
        public ICommand ConfigViewCommand { get; }
        public ICommand ProtRelayMonitorViewCommand { get; }
        public ICommand MqttViewCommad { get; }
        public ICommand ReportViewerCommand { get; }
        public ICommand AlarmViewCommand { get; }
        public ICommand UserConfigCommand { get; }
        public ICommand LicenseInfoCommand { get; }
        public ObservableCollection<HomeScreenTile> ScreenTiles { get; } = new();

        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigation;
        private readonly IAuthService? _auth;
        private readonly DeviceRuntimeService _deviceRuntime;
        private readonly AlarmMonitoringService _alarmService;
        private string _averageVoltage = "--";
        private string _totalActivePower = "--";
        private string _deviceSummary = "0 / 0";

        public string AverageVoltage { get => _averageVoltage; private set => SetProperty(ref _averageVoltage, value); }
        public string TotalActivePower { get => _totalActivePower; private set => SetProperty(ref _totalActivePower, value); }
        public string DeviceSummary { get => _deviceSummary; private set => SetProperty(ref _deviceSummary, value); }
        public int CriticalAlarmCount => _alarmService.CriticalAlarmCount;
        public int ActiveAlarmCount => _alarmService.ActiveAlarmCount;

        public HomePageViewModel(INavigationService navigation, IDialogService dialogService,
            DeviceRuntimeService deviceRuntime, AlarmMonitoringService alarmService, IAuthService? auth = null)
        {
            _navigation = navigation;
            _dialogService = dialogService;
            _auth = auth;
            _deviceRuntime = deviceRuntime;
            _alarmService = alarmService;

            SldViewCommand = new RelayCommand(SLDViewLogin);
            EnergyViewCommand = new RelayCommand(() => _navigation.NavigateTo<EnergyMonitorViewModel>());
            GaugeViewCommand = new RelayCommand(() => _navigation.NavigateTo<EnergyMonitorViewModel2>());
            ConfigViewCommand = new RelayCommand(() => _navigation.NavigateTo<ConfigurationViewModel>());
            ProtRelayMonitorViewCommand = new RelayCommand(() => _navigation.NavigateTo<Iec61850MonitorViewModel>());
            MqttViewCommad = new RelayCommand(() => _navigation.NavigateTo<MqttMonitorViewModel>());
            ReportViewerCommand = new RelayCommand(() => _navigation.NavigateTo<ReportViewerViewModel>());
            AlarmViewCommand = new RelayCommand(() => _navigation.NavigateTo<AlarmViewModel>());
            UserConfigCommand = new RelayCommand(() => _navigation.NavigateTo<UserSettingsViewModel>());
            LicenseInfoCommand = new RelayCommand(() => _navigation.NavigateTo<LicenseInfoViewModel>());
            RebuildScreenTiles();

            // subscribe to auth changes to update visibility properties
            if (_auth != null)
                _auth.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(_auth.CurrentUser))
                    {
                        RaiseAllVisibility();
                        RebuildScreenTiles();
                    }
                };

            _deviceRuntime.SnapshotUpdated += RefreshLiveSummary;
            _alarmService.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(_alarmService.ActiveAlarmCount) or nameof(_alarmService.CriticalAlarmCount))
                    RunOnUi(() =>
                    {
                        OnPropertyChanged(nameof(ActiveAlarmCount));
                        OnPropertyChanged(nameof(CriticalAlarmCount));
                    });
            };
            RefreshLiveSummary();
        }

        private void RefreshLiveSummary()
        {
            var readings = _deviceRuntime.GetSnapshot();
            var voltages = readings.Values.SelectMany(x => x.Values)
                .Where(x => IsVoltageAverage(x.Key) && TryFinite(x.Value, out _))
                .Select(x => Convert.ToDouble(x.Value, CultureInfo.InvariantCulture)).ToList();
            var powers = readings.Values.SelectMany(x => x.Values)
                .Where(x => IsActivePower(x.Key) && TryFinite(x.Value, out _))
                .Select(x => Convert.ToDouble(x.Value, CultureInfo.InvariantCulture)).ToList();
            var responding = readings.Values.Count(x => x.Values.Any(v => v.Value != null));
            var configured = readings.Count;
            RunOnUi(() =>
            {
                AverageVoltage = voltages.Count == 0 ? "--" : voltages.Average().ToString("F1", CultureInfo.InvariantCulture);
                TotalActivePower = powers.Count == 0 ? "--" : powers.Sum().ToString("F2", CultureInfo.InvariantCulture);
                DeviceSummary = $"{responding} / {configured}";
            });
        }

        private static bool IsVoltageAverage(string name)
        {
            var key = Normalize(name);
            return key is "voltagelnavg" or "averagevoltage" or "voltageavg";
        }

        private static bool IsActivePower(string name)
        {
            var key = Normalize(name);
            return key is "totalactivepower" or "activepower";
        }

        private static string Normalize(string value) =>
            new((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

        private static bool TryFinite(object value, out double result)
        {
            try { result = Convert.ToDouble(value, CultureInfo.InvariantCulture); return !double.IsNaN(result) && !double.IsInfinity(result); }
            catch { result = 0; return false; }
        }

        private static void RunOnUi(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess()) action(); else dispatcher.BeginInvoke(action);
        }

        private void SLDViewLogin() => _navigation.NavigateTo<Dashboard1ViewModel>();

        // Exposed properties used by XAML for visibility
        public bool CanSeeUserConfig => _auth?.CurrentUser != null && _auth.CurrentUser.Role == IEC.Shared.Models.UserRole.Admin;

        // the main app screens are visible to Supervisor and Operator as well; Admin can see them too
        public bool CanSeeMainScreens => _auth?.CurrentUser != null;
        private IEC.Shared.Models.ScreenPermissions Permissions => _auth?.CurrentUser?.ScreenPermissions ?? new();
        public bool CanSeeSld => CanSeeMainScreens && Permissions.SldView;
        public bool CanSeeEnergy => CanSeeMainScreens && Permissions.EnergyMonitor;
        public bool CanSeeGauge => CanSeeMainScreens && Permissions.GaugeView;
        public bool CanSeeDeviceConfig => CanSeeMainScreens && Permissions.DeviceConfiguration;
        public bool CanSeeRelay => CanSeeMainScreens && Permissions.RelayMonitor;
        public bool CanSeeRemote => CanSeeMainScreens && Permissions.RemoteView;
        public bool CanSeeReports => CanSeeMainScreens && Permissions.Reports;
        public bool CanSeeAlarms => CanSeeMainScreens && Permissions.Alarms;
        public bool CanSeeUserSettings => CanSeeMainScreens && Permissions.UserConfiguration;
        public string CurrentUsername => _auth?.CurrentUser?.Username ?? "Not signed in";

        // Helper to raise change notifications for the properties bound to UI
        private void RaiseAllVisibility()
        {
            OnPropertyChanged(nameof(CanSeeUserConfig));
            OnPropertyChanged(nameof(CanSeeMainScreens));
            OnPropertyChanged(nameof(CurrentUsername));
            OnPropertyChanged(nameof(CanSeeSld)); OnPropertyChanged(nameof(CanSeeEnergy));
            OnPropertyChanged(nameof(CanSeeGauge)); OnPropertyChanged(nameof(CanSeeDeviceConfig));
            OnPropertyChanged(nameof(CanSeeRelay)); OnPropertyChanged(nameof(CanSeeRemote));
            OnPropertyChanged(nameof(CanSeeReports)); OnPropertyChanged(nameof(CanSeeAlarms));
            OnPropertyChanged(nameof(CanSeeUserSettings));
        }

        private void RebuildScreenTiles()
        {
            RunOnUi(() =>
            {
                ScreenTiles.Clear();
                if (CanSeeSld) ScreenTiles.Add(new("SLD View", "\uE968", SldViewCommand));
                if (CanSeeEnergy) ScreenTiles.Add(new("Energy Monitor", "\uE945", EnergyViewCommand));
                if (CanSeeGauge) ScreenTiles.Add(new("Gauge View", "\uE9D9", GaugeViewCommand));
                if (CanSeeDeviceConfig) ScreenTiles.Add(new("Device Config", "\uE713", ConfigViewCommand));
                if (CanSeeRelay) ScreenTiles.Add(new("Relay Monitor", "\uE7F4", ProtRelayMonitorViewCommand));
                if (CanSeeRemote) ScreenTiles.Add(new("Remote View", "\uE774", MqttViewCommad));
                if (CanSeeReports) ScreenTiles.Add(new("Reports", "\uE9D2", ReportViewerCommand));
                if (CanSeeAlarms) ScreenTiles.Add(new("Alarms", "\uE7BA", AlarmViewCommand, true));
                if (CanSeeUserSettings) ScreenTiles.Add(new("User Config", "\uE77B", UserConfigCommand));
                if (CanSeeMainScreens) ScreenTiles.Add(new("License Info", "\uE946", LicenseInfoCommand));
            });
        }
    }

    public class HomeScreenTile
    {
        public HomeScreenTile(string title, string icon, ICommand command, bool isDanger = false)
        { Title = title; Icon = icon; Command = command; IsDanger = isDanger; }
        public string Title { get; }
        public string Icon { get; }
        public ICommand Command { get; }
        public bool IsDanger { get; }
    }
}
