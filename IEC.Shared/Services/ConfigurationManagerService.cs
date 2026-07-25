using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IEC.Shared.Models;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization; // required for JsonStringEnumConverter
using System.Collections.ObjectModel;

namespace IEC.Shared.Services
{
    public class ConfigurationManagerService
    {
        private string _configFile;

        public ProjectConfiguration Configuration { get; private set; }

        public ConfigurationManagerService()
        {
            _configFile = GetConfigFilePath();
            Load();
        }

        //-------------------------------------------------------

        public void Load()
        {
            // If preferred solution-level file doesn't exist, try the bin-level fallback.
            if (!File.Exists(_configFile))
            {
                var fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                            "Configuration",
                                            "ProjectConfig.json");

                if (File.Exists(fallback))
                    _configFile = fallback;
            }

            if (!File.Exists(_configFile))
            {
                Configuration = CreateDefaultConfiguration();
                Save();
                return;
            }

            string json = File.ReadAllText(_configFile);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Allow enums to be represented as strings in JSON (backwards-compatible if you previously stored strings)
            options.Converters.Add(new JsonStringEnumConverter());

            Configuration =
                JsonSerializer.Deserialize<ProjectConfiguration>(json, options)
                ?? CreateDefaultConfiguration();

            // Ensure collections and nested objects are non-null so the UI bindings work.
            if (Configuration.Meters == null)
                Configuration.Meters = new List<MetersConfig>();

            foreach (var meter in Configuration.Meters)
            {
                if (meter.Communication == null)
                    meter.Communication = new CommunicationConfig();

                if (meter.Registers == null)
                    meter.Registers = new ObservableCollection<RegisterConfig>();
            }
        }

        //-------------------------------------------------------

        public void Save()
        {
            string folder = Path.GetDirectoryName(_configFile)!;

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            // Serialize enums as strings to keep JSON readable and compatible
            options.Converters.Add(new JsonStringEnumConverter());

            string json =
                JsonSerializer.Serialize(Configuration, options);

            File.WriteAllText(_configFile, json);
        }

        //-------------------------------------------------------

        private ProjectConfiguration CreateDefaultConfiguration()
        {
            return new ProjectConfiguration();
        }

        // Attempts to find the solution folder by walking parent directories and
        // returns the preferred solution-level config path if found; otherwise falls back
        // to the bin-level config path.
        private string GetConfigFilePath()
        {
            try
            {
                var start = AppContext.BaseDirectory;
                var dir = new DirectoryInfo(start);

                while (dir != null)
                {
                    // Found a .sln file => treat this as the solution root
                    if (dir.EnumerateFiles("*.sln").Any())
                    {
                        return Path.Combine(dir.FullName, "Configuration", "ProjectConfig.json");
                    }

                    dir = dir.Parent;
                }
            }
            catch
            {
                // ignore and fallback below
            }

            // Default fallback to bin path
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                 "Configuration",
                                 "ProjectConfig.json");
        }
    }
}
