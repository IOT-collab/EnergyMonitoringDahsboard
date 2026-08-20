using IEC.Shared.Models;
using System;
using System.IO;
using System.Text.Json;

namespace IEC.Shared.Services;

public sealed class MqttConfigurationService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public MqttConfiguration Current { get; private set; } = new();

    public MqttConfigurationService()
    {
        Load();
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(AppPaths.MqttConfigurationFile))
            {
                Current = new MqttConfiguration();
                Save();
                return;
            }

            Current = JsonSerializer.Deserialize<MqttConfiguration>(
                File.ReadAllText(AppPaths.MqttConfigurationFile), _jsonOptions)
                ?? new MqttConfiguration();
        }
        catch
        {
            Current = new MqttConfiguration();
        }

        Current.Subscriptions ??= new();
        Current.FieldMappings ??= new();
        if (Current.Subscriptions.Count == 0)
            Current.Subscriptions.Add(new MqttSubscriptionConfig());
    }

    public bool Save()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.Configuration);
            File.WriteAllText(AppPaths.MqttConfigurationFile,
                JsonSerializer.Serialize(Current, _jsonOptions));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
