using IEC.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace IECGUI.Services;

/// <summary>Stores named industrial SLD definitions independently from the generated page widgets.</summary>
public sealed class IndustrialSldDefinitionStore
{
    private readonly string _filePath;

    public IndustrialSldDefinitionStore(IEC.Shared.Services.ConfigurationManagerService configuration)
    {
        var folder = Path.GetDirectoryName(configuration.ConfigurationFilePath)
            ?? AppContext.BaseDirectory;
        _filePath = Path.Combine(folder, "IndustrialSldDefinitions.json");
    }

    public List<IndustrialSldDefinition> Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return new List<IndustrialSldDefinition>();
            return JsonSerializer.Deserialize<List<IndustrialSldDefinition>>(File.ReadAllText(_filePath))
                ?? new List<IndustrialSldDefinition>();
        }
        catch
        {
            return new List<IndustrialSldDefinition>();
        }
    }

    public bool Save(IEnumerable<IndustrialSldDefinition> definitions)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var json = JsonSerializer.Serialize(definitions?.ToList() ?? new List<IndustrialSldDefinition>(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}