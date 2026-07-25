using IEC.Shared.IECModels;
using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;


namespace IEC.Shared.IECServices
{
    public class IecConfigManagerService
    {
        private readonly string _configFilePath;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public IecConfigManagerService()
        {
            
            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            _configFilePath = Path.Combine(appFolder, "iec61850_config.json");
        }

        // ── Load ──────────────────────────────────────────────
        public IecConfigRoot Load()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                
                    var defaultConfig = new IecConfigRoot();
                    defaultConfig.Relays.Add(IecDefaultConfig.GetDefault());
                    Save(defaultConfig);
                    return defaultConfig;
                }

                string json = File.ReadAllText(_configFilePath);
                return JsonSerializer.Deserialize<IecConfigRoot>(json, _jsonOptions)
                       ?? new IecConfigRoot();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IecConfig] Load failed: {ex.Message}");
                return new IecConfigRoot();
            }
        }

        // ── Save ──────────────────────────────────────────────
        public void Save(IecConfigRoot config)
        {
            try
            {
                string json = JsonSerializer.Serialize(config, _jsonOptions);
                File.WriteAllText(_configFilePath, json);
                Console.WriteLine($"[IecConfig] Saved to: {_configFilePath}");
                MessageBox.Show($"[IecConfig] Saved to: {_configFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IecConfig] Save failed: {ex.Message}");
                MessageBox.Show($"[IecConfig] Save failed: {ex.Message}");
            }
        }

        
        public string ConfigFilePath => _configFilePath;
    }
}