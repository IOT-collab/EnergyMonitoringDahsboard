using IECGUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using IEC.Shared.Services;

namespace IECGUI.ViewModel
{
    public class MainWindowViewModel : BaseViewModel
    {

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
