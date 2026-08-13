using IEC.Shared.Services;
using IECGUI.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using IEC.Shared.Models;

namespace IECGUI.ViewModel
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigation;
        private readonly IUserSettingsService _userService;
        private readonly IAuthService _authService;

        public ICommand LoginCommand { get; }

        public string Username { get => _username; set => SetProperty(ref _username, value); }
        private string _username;

        public string Password { get => _password; set => SetProperty(ref _password, value); }
        private string _password;

        public LoginViewModel(INavigationService navigation, IDialogService dialogService, IUserSettingsService userService, IAuthService authService)
        {
            _navigation = navigation;
            _dialogService = dialogService;
            _userService = userService;
            _authService = authService;

            LoginCommand = new RelayCommand(async () => await ExecuteLoginAsync());
        }

        public virtual async Task ExecuteLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                _dialogService.ShowWarning("Please enter username and password.");
                return;
            }

            var settings = _userService.Load();
            var user = settings.Users.FirstOrDefault(u => u.Username == Username && u.IsEnabled);

            if (user != null && _userService.VerifyPassword(user, Password))
            {
                // set current user in auth service (notifying subscribers)
                _authService.CurrentUser = new UserAccount
                {
                    Username = user.Username,
                    Password = string.Empty,
                    Role = user.Role,
                    IsEnabled = user.IsEnabled
                    ,ScreenPermissions = user.ScreenPermissions ?? ScreenPermissions.ForRole(user.Role)
                };

                _navigation.NavigateTo<HomePageViewModel>();
            }
            else
            {
                _dialogService.ShowWarning("Invalid username or password.");
            }

            Username = "";
            Password = "";
        }
    }
}
