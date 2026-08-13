using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace IEC.Shared.Models
{
    public class UserAccount : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set { if (_username == value) return; _username = value; Notify(); }
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set { if (_password == value) return; _password = value; Notify(); }
        }

        private string _newPassword = string.Empty;
        [JsonIgnore]
        public string NewPassword
        {
            get => _newPassword;
            set { if (_newPassword == value) return; _newPassword = value; Notify(); }
        }

        private UserRole _role = UserRole.Operator;
        public UserRole Role
        {
            get => _role;
            set { if (_role == value) return; _role = value; Notify(); }
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set { if (_isEnabled == value) return; _isEnabled = value; Notify(); }
        }
    }
}
