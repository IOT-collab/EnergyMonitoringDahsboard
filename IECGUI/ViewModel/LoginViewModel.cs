using IEC.Shared.Services;
using IECGUI.Services;
using Microsoft.Xaml.Behaviors.Layout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace IECGUI.ViewModel
{
    public class LoginViewModel : BaseViewModel
    {


        public ICommand LoginCommand { get; }

        private readonly IDialogService _dialogService;
 
        public string Username { get => _username; set => SetProperty(ref _username, value); }
        private string _username;

        public string Password { get => _password; set => SetProperty(ref _password, value); }
        private string _password;

        public string AppName { get; }

        private readonly bool _isLoading;

  
        private readonly MainWindowViewModel _mainVM;

        private readonly INavigationService _navigation;

        private bool _isUsernameFocused;

        public bool IsUsernameFocused
        {
            get => _isUsernameFocused;
            set => SetProperty(ref _isUsernameFocused, value);

        }
        public LoginViewModel(INavigationService navigation , IDialogService dialogService)
        { 

            _navigation = navigation;
            _dialogService = dialogService;
            
            LoginCommand = new RelayCommand(async () => await ExecuteLoginAsync());

        }

        public virtual async Task ExecuteLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                _dialogService.ShowWarning("Please enter username and password.");


                return;
            }


            try
            {
                if (Username == "admin" && Password == "admin")
                {
                    // Successful login
           
                    _navigation.NavigateTo<HomePageViewModel>();
                    //_navigation.NavigateTo(new MainWindowViewModel());


                }
                else
                {
                    // Failed login
                    _dialogService.ShowWarning("Invalid username or password.");
        
                }

                Username = "";Password = "";

            }
            catch (System.Exception ex)
            {

            }

        }


    }
}
