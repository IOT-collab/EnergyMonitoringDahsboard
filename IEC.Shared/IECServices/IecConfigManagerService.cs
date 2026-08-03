using IEC.Shared.IECModels;
using System;
using System.IO;
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
        // Constructor
        //---------------------------------------------------------

        public IecConfigManagerService()
        {
            _configFilePath = AppPaths.IecConfigurationFile;

            // Copy default configuration from installer on first run
            string defaultFile =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "Configuration",
                    "iec61850_config.json");

            AppPaths.EnsureDefaultFile(_configFilePath, defaultFile);
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
                
                MessageBox.Show(ex.Message,"IEC Configuration", MessageBoxButton.OK, MessageBoxImage.Error);

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

        public string ConfigFilePath => _configFilePath;
    }


}