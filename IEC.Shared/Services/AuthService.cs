using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using IEC.Shared.Models;

namespace IEC.Shared.Services
{
    // Simple in-memory auth/session service. Set CurrentUser at login.
    public class AuthService : IAuthService
    {
        private UserAccount? _currentUser;
        public UserAccount? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (_currentUser == value) return;
                _currentUser = value;
                Notify();
            }
        }

        public bool IsInRole(UserRole role) => CurrentUser != null && CurrentUser.Role == role;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}