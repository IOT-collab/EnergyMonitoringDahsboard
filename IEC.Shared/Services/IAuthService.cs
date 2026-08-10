using System;
using System.ComponentModel;
using IEC.Shared.Models;

namespace IEC.Shared.Services
{
    public interface IAuthService : INotifyPropertyChanged
    {
        UserAccount? CurrentUser { get; set; }
        bool IsInRole(UserRole role);
    }
}