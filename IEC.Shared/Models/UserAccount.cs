using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IEC.Shared.Models
{
    // NOTE: For production store salted password hashes, not plain text.
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

        private string _password = string.Empty; // placeholder - replace with hashed storage
        public string Password
        {
            get => _password;
            set { if (_password == value) return; _password = value; Notify(); }
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