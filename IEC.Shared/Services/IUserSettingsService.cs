using IEC.Shared.Models;

namespace IEC.Shared.Services
{
    public interface IUserSettingsService
    {
        UserSettings Load();
        bool Save(UserSettings settings);
        string GetFilePath();
        bool VerifyPassword(UserAccount user, string password);
        string HashPassword(string password);
    }
}
