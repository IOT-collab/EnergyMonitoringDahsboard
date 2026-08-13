using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using IEC.Shared.Models;

namespace IEC.Shared.Services
{
    // Simple file-backed user settings service (separate JSON file)
    public class UserSettingsService : IUserSettingsService
    {
        private const string HashPrefix = "PBKDF2-SHA256";
        private const int Iterations = 210000;
        private const int SaltSize = 16;
        private const int HashSize = 32;
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
                    Password = HashPassword("admin"),
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
                var migrated = false;
                foreach (var user in settings.Users)
                {
                    if (!string.IsNullOrEmpty(user.Password) && !IsPasswordHash(user.Password))
                    {
                        user.Password = HashPassword(user.Password);
                        migrated = true;
                    }
                }
                if (migrated) Save(settings);
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
                foreach (var user in settings.Users)
                {
                    if (!string.IsNullOrWhiteSpace(user.NewPassword))
                        user.Password = HashPassword(user.NewPassword);
                    else if (!string.IsNullOrEmpty(user.Password) && !IsPasswordHash(user.Password))
                        user.Password = HashPassword(user.Password);
                    user.NewPassword = string.Empty;
                }
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

        public string HashPassword(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations,
                HashAlgorithmName.SHA256, HashSize);
            return $"{HashPrefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public bool VerifyPassword(UserAccount user, string password)
        {
            if (user == null || password == null || string.IsNullOrWhiteSpace(user.Password) ||
                !TryReadHash(user.Password, out var iterations, out var salt, out var expected))
                return false;
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations,
                HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        private static bool IsPasswordHash(string value) =>
            value.StartsWith(HashPrefix + "$", StringComparison.Ordinal);

        private static bool TryReadHash(string value, out int iterations, out byte[] salt, out byte[] hash)
        {
            iterations = 0; salt = Array.Empty<byte>(); hash = Array.Empty<byte>();
            try
            {
                var parts = value.Split('$');
                if (parts.Length != 4 || parts[0] != HashPrefix ||
                    !int.TryParse(parts[1], out iterations) || iterations < 100000)
                    return false;
                salt = Convert.FromBase64String(parts[2]);
                hash = Convert.FromBase64String(parts[3]);
                return salt.Length >= SaltSize && hash.Length >= HashSize;
            }
            catch { return false; }
        }
    }
}
