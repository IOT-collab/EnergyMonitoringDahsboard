using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace IECGUI.ViewModel
{
    public class UserSettingsViewModel : BaseViewModel
    {
        private readonly IUserSettingsService _userService;
        private readonly IDialogService _dialog;
        private readonly INavigationService _navigation;

        private UserAccount _selectedUser;
        public UserAccount SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        public ObservableCollection<UserAccount> Users { get; } = new ObservableCollection<UserAccount>();

        public ObservableCollection<UserRole> Roles { get; } =
            new ObservableCollection<UserRole>(Enum.GetValues(typeof(UserRole)).Cast<UserRole>());

        public ICommand AddUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand HomeCommand { get; }

        public UserSettingsViewModel(INavigationService navigation, IUserSettingsService userService, IDialogService dialog)
        {
            _navigation = navigation;
            _userService = userService;
            _dialog = dialog;

            // load existing users from user settings file
            var settings = _userService.Load();
            Users.Clear();
            foreach (var u in settings.Users)
                Users.Add(new UserAccount
                {
                    Username = u.Username,
                    Password = u.Password,
                    NewPassword = string.Empty,
                    Role = u.Role,
                    IsEnabled = u.IsEnabled
                    ,ScreenPermissions = u.ScreenPermissions ?? ScreenPermissions.ForRole(u.Role)
                });

            AddUserCommand = new RelayCommand(AddUser);
            DeleteUserCommand = new RelayCommand(DeleteUser);
            SaveCommand = new RelayCommand(Save);
            HomeCommand = new RelayCommand(() => _navigation.NavigateTo<HomePageViewModel>());
        }

        private void AddUser()
        {
            int idx = Users.Count + 1;
            var user = new UserAccount
            {
                Username = $"user{idx}",
                Password = string.Empty,
                Role = UserRole.Operator,
                IsEnabled = true
                ,ScreenPermissions = ScreenPermissions.ForRole(UserRole.Operator)
            };

            Users.Add(user);
            SelectedUser = user;
        }

        private void DeleteUser()
        {
            if (SelectedUser == null)
            {
                _dialog.ShowMessage("Select a user to delete.", "Delete user");
                return;
            }

            if (_dialog.ShowYesNo($"Delete user '{SelectedUser.Username}'?", "Confirm delete"))
            {
                Users.Remove(SelectedUser);
                SelectedUser = null;
            }
        }

        private void Save()
        {
            var settings = new UserSettings();
            foreach (var u in Users)
            {
                settings.Users.Add(new UserAccount
                {
                    Username = u.Username,
                    Password = string.IsNullOrWhiteSpace(u.NewPassword)
                        ? u.Password
                        : _userService.HashPassword(u.NewPassword),
                    Role = u.Role,
                    IsEnabled = u.IsEnabled
                    ,ScreenPermissions = u.ScreenPermissions ?? ScreenPermissions.ForRole(u.Role)
                });
            }

               if (_userService.Save(settings))
                _dialog.ShowMessage("User settings saved.", "Saved");
            else
                _dialog.ShowMessage("Failed to save user settings.", "Error");
        }
    }
}

