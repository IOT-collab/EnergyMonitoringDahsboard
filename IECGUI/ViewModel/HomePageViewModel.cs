using IEC.Shared.Services;
using IECGUI.Services;
using System;
using System.Windows.Input;

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

        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigation;
        private readonly IAuthService? _auth;

        public HomePageViewModel(INavigationService navigation, IDialogService dialogService, IAuthService? auth = null)
        {
            _navigation = navigation;
            _dialogService = dialogService;
            _auth = auth;

            SldViewCommand = new RelayCommand(SLDViewLogin);
            EnergyViewCommand = new RelayCommand(() => _navigation.NavigateTo<EnergyMonitorViewModel>());
            GaugeViewCommand = new RelayCommand(() => _navigation.NavigateTo<EnergyMonitorViewModel2>());
            ConfigViewCommand = new RelayCommand(() => _navigation.NavigateTo<ConfigurationViewModel>());
            ProtRelayMonitorViewCommand = new RelayCommand(() => _navigation.NavigateTo<Iec61850MonitorViewModel>());
            MqttViewCommad = new RelayCommand(() => _navigation.NavigateTo<MqttMonitorViewModel>());
            ReportViewerCommand = new RelayCommand(() => _navigation.NavigateTo<ReportViewerViewModel>());
            AlarmViewCommand = new RelayCommand(() => _navigation.NavigateTo<AlarmViewModel>());
            UserConfigCommand = new RelayCommand(() => _navigation.NavigateTo<UserSettingsViewModel>());

            // subscribe to auth changes to update visibility properties
            if (_auth != null)
                _auth.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(_auth.CurrentUser))
                        RaiseAllVisibility();
                };
        }

        private void SLDViewLogin() => _navigation.NavigateTo<Dashboard1ViewModel>();

        // Exposed properties used by XAML for visibility
        public bool CanSeeUserConfig => _auth?.CurrentUser != null && _auth.CurrentUser.Role == IEC.Shared.Models.UserRole.Admin;

        // the main app screens are visible to Supervisor and Operator as well; Admin can see them too
        public bool CanSeeMainScreens => _auth?.CurrentUser != null;
        public string CurrentUsername => _auth?.CurrentUser?.Username ?? "Not signed in";

        // Helper to raise change notifications for the properties bound to UI
        private void RaiseAllVisibility()
        {
            OnPropertyChanged(nameof(CanSeeUserConfig));
            OnPropertyChanged(nameof(CanSeeMainScreens));
            OnPropertyChanged(nameof(CurrentUsername));
        }
    }
}
