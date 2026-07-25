using IEC.Shared.Services;
using IECGUI.Services;
using IPCSoftware.Common.CommonExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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

        private readonly SafePoller _liveDataTimer;
        public string AppVersion => "23.40.32";

        public INavigationService Navigation { get; }

        public ICommand CloseAppCommand { get; set; }
        public ICommand LogoutCommand { get; set; }

        public MainWindowViewModel(INavigationService navigation)
        {
            Navigation = navigation;

            // Forward NavigationService's CurrentView changes to this ViewModel's bindings
            Navigation.CurrentViewChanged += () => OnPropertyChanged(nameof(Navigation));

            CloseAppCommand = new RelayCommand(ExecuteCloseApp);
            LogoutCommand = new RelayCommand(ExecuteLogout);

            Navigation.NavigateTo<LoginViewModel>();
            _liveDataTimer = new SafePoller(TimeSpan.FromMilliseconds(1000), PollAsync, ex => Console.WriteLine(ex.Message));
            _liveDataTimer.Start();

            // Set initial time immediately
            SystemTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");
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
            if (MessageBox.Show("Are you sure you want to exit?", "Confirm Exit", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        private void ExecuteLogout()
        {
            if (MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Navigation.NavigateTo<LoginViewModel>();
            }
        }
    }
}
