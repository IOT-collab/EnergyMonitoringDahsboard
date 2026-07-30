using IEC.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IEC.Shared.Services
{
    public class ConfigurationManagerService
    {
        private readonly string _configFile;

        public ProjectConfiguration Configuration { get; private set; }

        public ConfigurationManagerService()
        {
            _configFile = GetConfigFilePath();
            Load();
        }

        //-------------------------------------------------------------

        public void Load()
        {
            if (!File.Exists(_configFile))
            {
                Configuration = CreateDefaultConfiguration();
                Save();
                return;
            }

            string json = File.ReadAllText(_configFile);

            var options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };

            options.Converters.Add(new JsonStringEnumConverter());

            Configuration =
                JsonSerializer.Deserialize<ProjectConfiguration>(json, options)
                ?? CreateDefaultConfiguration();

            if (Configuration.Meters == null)
                Configuration.Meters = new List<MetersConfig>();

            foreach (var meter in Configuration.Meters)
            {
                meter.Communication ??= new CommunicationConfig();
                meter.Registers ??= new ObservableCollection<RegisterConfig>();
            }
        }

        //-------------------------------------------------------------

        public bool Save()
        {
            try
            {
                string folder = Path.GetDirectoryName(_configFile)!;

                Directory.CreateDirectory(folder);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                options.Converters.Add(new JsonStringEnumConverter());

                File.WriteAllText(
                    _configFile,
                    JsonSerializer.Serialize(Configuration, options));

                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Unable to save configuration.\n\n{ex.Message}",
                    "Configuration Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);

                return false;
            }
        }

        //-------------------------------------------------------------

        private ProjectConfiguration CreateDefaultConfiguration()
        {
            return new ProjectConfiguration();
        }

        //-------------------------------------------------------------
        // Returns configuration path depending on environment.
        //-------------------------------------------------------------

        private string GetConfigFilePath()
        {
            //=========================================================
            // DEVELOPMENT MODE (Visual Studio)
            //=========================================================

            try
            {
                DirectoryInfo? dir =
                    new DirectoryInfo(AppContext.BaseDirectory);

                while (dir != null)
                {
                    if (dir.GetFiles("*.sln").Any())
                    {
                        string configFolder =
                            Path.Combine(dir.FullName, "Configuration");

                        Directory.CreateDirectory(configFolder);

                        return Path.Combine(
                            configFolder,
                            "ProjectConfig.json");
                    }

                    dir = dir.Parent;
                }
            }
            catch
            {
                // Ignore and continue to installed mode.
            }

            //=========================================================
            // INSTALLED APPLICATION
            //=========================================================

            string programDataFolder =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.CommonApplicationData),
                    "VEMT",
                    "Configuration");

            Directory.CreateDirectory(programDataFolder);

            string userConfig =
                Path.Combine(
                    programDataFolder,
                    "ProjectConfig.json");

            //---------------------------------------------------------
            // First Run
            //---------------------------------------------------------

            if (!File.Exists(userConfig))
            {
                string defaultConfig =
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "Configuration",
                        "ProjectConfig.json");

                if (File.Exists(defaultConfig))
                {
                    File.Copy(defaultConfig, userConfig);
                }
            }

            return userConfig;
        }
    }
}