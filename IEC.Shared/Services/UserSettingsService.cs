using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using IEC.Shared.Models;

namespace IEC.Shared.Services
{
    // Simple file-backed user settings service (separate JSON file)
    public class UserSettingsService : IUserSettingsService
    {
        private readonly string _filePath;

        public UserSettingsService()
        {
            // Store next to application configuration folder
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "VEMT",
                "UserSettings");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "UserSettings.json");

            // If missing, create default admin
            if (!File.Exists(_filePath))
            {
                var defaultSettings = new UserSettings();
                defaultSettings.Users.Add(new UserAccount
                {
                    Username = "admin",
                    Password = "admin", // NOTE: replace with hash in production
                    Role = UserRole.Admin,
                    IsEnabled = true
                });
                Save(defaultSettings);
            }
        }

        public string GetFilePath() => _filePath;

        public UserSettings Load()
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new JsonStringEnumConverter());
                var json = File.ReadAllText(_filePath);
                var settings = JsonSerializer.Deserialize<UserSettings>(json, options) ?? new UserSettings();
                // ensure list not null
                settings.Users ??= new System.Collections.Generic.List<UserAccount>();
                return settings;
            }
            catch
            {
                return new UserSettings();
            }
        }

        public bool Save(UserSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new JsonStringEnumConverter());
                File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, options));
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}