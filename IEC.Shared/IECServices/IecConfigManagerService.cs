using IEC.Shared.IECModels;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace IEC.Shared.IECServices
{
    public class IecConfigManagerService
    {
        private readonly string _configFilePath;

        private readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };

        //---------------------------------------------------------

        public IecConfigManagerService()
        {
            _configFilePath = GetConfigFilePath();
        }

        //---------------------------------------------------------
        // Load
        //---------------------------------------------------------

        public IecConfigRoot Load()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    var config = new IecConfigRoot();
                    config.Relays.Add(IecDefaultConfig.GetDefault());

                    Save(config);

                    return config;
                }

                string json = File.ReadAllText(_configFilePath);

                return JsonSerializer.Deserialize<IecConfigRoot>(
                    json,
                    _jsonOptions)
                    ?? new IecConfigRoot();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "IEC Configuration",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return new IecConfigRoot();
            }
        }

        //---------------------------------------------------------
        // Save
        //---------------------------------------------------------

        public bool Save(IecConfigRoot config)
        {
            try
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(_configFilePath)!);

                string json =
                    JsonSerializer.Serialize(
                        config,
                        _jsonOptions);

                File.WriteAllText(_configFilePath, json);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to save IEC configuration.\n\n{ex.Message}",
                    "IEC Configuration",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
        }

        //---------------------------------------------------------
        // Configuration Path
        //---------------------------------------------------------

        private string GetConfigFilePath()
        {
            //-----------------------------------------------------
            // Visual Studio Development
            //-----------------------------------------------------

            try
            {
                DirectoryInfo? dir =
                    new DirectoryInfo(AppContext.BaseDirectory);

                while (dir != null)
                {
                    if (dir.GetFiles("*.sln").Any())
                    {
                        string folder =
                            Path.Combine(
                                dir.FullName,
                                "Configuration");

                        Directory.CreateDirectory(folder);

                        return Path.Combine(
                            folder,
                            "iec61850_config.json");
                    }

                    dir = dir.Parent;
                }
            }
            catch
            {
            }

            //-----------------------------------------------------
            // Installed Application
            //-----------------------------------------------------

            string configFolder =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.CommonApplicationData),
                    "VEMT",
                    "Configuration");

            Directory.CreateDirectory(configFolder);

            string userConfig =
                Path.Combine(
                    configFolder,
                    "iec61850_config.json");

            //-----------------------------------------------------
            // First Run
            //-----------------------------------------------------

            if (!File.Exists(userConfig))
            {
                string defaultConfig =
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "Configuration",
                        "iec61850_config.json");

                if (File.Exists(defaultConfig))
                {
                    File.Copy(defaultConfig, userConfig);
                }
            }

            return userConfig;
        }

        //---------------------------------------------------------

        public string ConfigFilePath => _configFilePath;
    }
}