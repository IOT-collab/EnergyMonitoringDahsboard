using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace IEC.Shared.Models;

public enum ScadaWidgetType
{
    Label,
    Value,
    Led,
    Button,
    Line,
    Rectangle,
    Circle,
    Breaker,
    Image
}

public sealed class ScadaPageConfig : INotifyPropertyChanged
{
    private string _pageName = "Main Plant";
    public string PageName
    {
        get => _pageName;
        set
        {
            if (_pageName == value) return;
            _pageName = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PageName)));
        }
    }

    public double CanvasWidth { get; set; } = 1500;
    public double CanvasHeight { get; set; } = 800;
    public List<ScadaWidgetConfig> Widgets { get; set; } = new();
    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class ScadaWidgetConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string? GroupId { get; set; }
    public ScadaWidgetType Type { get; set; } = ScadaWidgetType.Label;
    public string Caption { get; set; } = "New object";
    public string OnCaption { get; set; } = "ON";
    public string OffCaption { get; set; } = "OFF";
    public double X { get; set; } = 80;
    public double Y { get; set; } = 80;
    public double Width { get; set; } = 220;
    public double Height { get; set; } = 60;
    public double Rotation { get; set; }
    public double LineThickness { get; set; } = 3;
    // Optional animated flow overlay for line widgets.
    public bool FlowAnimationEnabled { get; set; }
    public bool FlowOnlyWhenOn { get; set; } = true;
    public string FlowDirection { get; set; } = "Forward";
    public double FontSize { get; set; } = 20;
    public string FontWeight { get; set; } = "SemiBold";
    public string? ImagePath { get; set; }
    // Optional IEC SLD variant: ACB, VCB, Disconnector, Fuse, Transformer, CT/VT, etc.
    public string SymbolKind { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? ParameterName { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Foreground { get; set; } = "#E6F8FF";
    public string Background { get; set; } = "#19364A";
    public bool DynamicStateColors { get; set; }
    public string OnForeground { get; set; } = "#FFFFFF";
    public string OnBackground { get; set; } = "#18A957";
    public string OffForeground { get; set; } = "#D7E7EE";
    public string OffBackground { get; set; } = "#5A2A35";
    public bool IsVisible { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public bool ConditionEnabled { get; set; }
    public string? ConditionDeviceName { get; set; }
    public string? ConditionParameterName { get; set; }
    public string ConditionOperator { get; set; } = "Always";
    public string ConditionValue { get; set; } = string.Empty;
    public bool IsMomentary { get; set; }
    public double Minimum { get; set; }
    public double Maximum { get; set; } = 100;
}

public sealed class IndustrialSldDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Substation SLD";
    public string VoltageLevel { get; set; } = "MV";
    public List<string> IncomerNames { get; set; } = new();
    public List<string> BusCouplerNames { get; set; } = new();
    public List<string> OutgoingNames { get; set; } = new();
}
public sealed class ScadaSymbolLibraryItem : INotifyPropertyChanged
{
    private bool _isFavorite;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "IEC symbol";
    public string VoltageLevel { get; set; } = "MV";
    public string Kind { get; set; } = "Incomer";
    public string? FilePath { get; set; }
    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (_isFavorite == value) return;
            _isFavorite = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFavorite)));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}
