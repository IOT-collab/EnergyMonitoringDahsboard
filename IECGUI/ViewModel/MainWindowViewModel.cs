using IEC.Shared.Services;
using IECGUI.Services;
using IEC.CommonService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace IECGUI.ViewModel
{
    public class MainWindowViewModel : BaseViewModel
    {

        // Live System Time Property
        private string _systemTime;
        public string SystemTime
        {
            get => _systemTime;
            set => SetProperty(ref _systemTime, value);
        }
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }


        private readonly SafePoller _liveDataTimer;
        public string AppVersion => "23.40.32";

        public INavigationService Navigation { get; }
        public AlarmMonitoringService AlarmService { get; }

        public ICommand CloseAppCommand { get; set; }
        public ICommand LogoutCommand { get; set; }

        public ICommand MinimizeCommand { get; set; }
        public Visibility SessionControlsVisibility { get; private set; }

        private readonly IDialogService _dialogService;
        private readonly LicenseService _licenseService;
        private readonly ScadaDesignerViewModel _scadaDesigner;

        public MainWindowViewModel(INavigationService navigation,
            IDialogService dialogService,
            AlarmMonitoringService alarmService,
            DeviceRuntimeService deviceRuntime,
            LicenseService licenseService,
            ScadaDesignerViewModel scadaDesigner)
        {
            Navigation = navigation;
            _dialogService = dialogService;
            AlarmService = alarmService;
            _licenseService = licenseService;
            _scadaDesigner = scadaDesigner;
            SessionControlsVisibility = _licenseService.Current.CanRun
                ? Visibility.Visible
                : Visibility.Collapsed;
            _licenseService.StatusChanged += OnLicenseStatusChanged;

            // Forward NavigationService's CurrentView changes to this ViewModel's bindings
            Navigation.CurrentViewChanged += () => { OnPropertyChanged(nameof(Navigation)); isLoginView(); }; 

            

            _currentView = Navigation.CurrentView;
                        
            CloseAppCommand = new RelayCommand(ExecuteCloseApp);
            LogoutCommand = new RelayCommand(ExecuteLogout);
            MinimizeCommand = new RelayCommand(ExecuteMinimize);

           

            if (_licenseService.Current.CanRun)
            {
                var licenseMessage = _licenseService.Current.Message;
                Application.Current.Dispatcher.BeginInvoke(
                    DispatcherPriority.ApplicationIdle,
                    new Action(() => _dialogService.ShowWarning(licenseMessage)));
                _ = StartRuntimeAsync(deviceRuntime);
                Navigation.NavigateTo<LoginViewModel>();
            }
            else
            {
                Navigation.NavigateTo<LicenseActivationViewModel>();
            }
            _liveDataTimer = new SafePoller(TimeSpan.FromMilliseconds(1000), PollAsync, ex => Console.WriteLine(ex.Message));
            _liveDataTimer.Start();

            // Set initial time immediately
            SystemTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");

            isLoginView(); // Initial state
        }

        private async Task StartRuntimeAsync(DeviceRuntimeService deviceRuntime)
        {
            try
            {
                await deviceRuntime.StartAsync();
                await AlarmService.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Alarm monitoring startup: {ex.Message}");
            }
        }

        private async Task PollAsync(Dictionary<int, object> parameters)
        {
            try
            {
                SystemTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");
            }
            catch
            {

            }

        }

        private void ExecuteCloseApp()
        {
            Application.Current.MainWindow?.Close();
        }

        public bool ConfirmClose()
        {
            if (_scadaDesigner.HasUnsavedChanges)
            {
                var choice = _dialogService.ShowYesNoCancel(
                    "You have some unsaved changes.\n Do you want to save changes?",
                    "Unsaved SCADA changes");

                if (choice == CustomMessageBoxResult.Cancel)
                    return false;

                if (choice == CustomMessageBoxResult.Yes && !_scadaDesigner.TrySaveChanges())
                {
                    _dialogService.ShowMessage("The SCADA layout could not be saved. The application will remain open.", "Save failed");
                    return false;
                }

                return true;
            }

            return _dialogService.ShowYesNo("Are you sure you want to exit?", "Confirm Exit");
        }

        private void ExecuteLogout()
        {
            // This command is also guarded in code so it cannot be invoked by
            // automation or stale UI while the license gate is active.
            if (!_licenseService.Validate().CanRun)
                return;

            if (_dialogService.ShowYesNo("Are you sure you want to logout?", "Confirm Logout") == true)
            {
                Navigation.NavigateTo<LoginViewModel>();
              
            }
        }

        private void ExecuteMinimize()
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void isLoginView()
        {
            SessionControlsVisibility = (Navigation.CurrentView is LoginViewModel) ? Visibility.Collapsed : Visibility.Visible;
            OnPropertyChanged(nameof(SessionControlsVisibility));
        }

        private void OnLicenseStatusChanged(LicenseStatus status)
        {
            SessionControlsVisibility = status.CanRun
                ? Visibility.Visible
                : Visibility.Collapsed;
            OnPropertyChanged(nameof(SessionControlsVisibility));
        }
    }
}
