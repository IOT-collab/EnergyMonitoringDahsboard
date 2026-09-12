using System;
using System.Collections.Generic;

namespace IEC.Shared.Models;

public enum ScadaWidgetType
{
    Label,
    Value,
    Led,
    Button,
    Line,
    Rectangle,
    Circle
}

public sealed class ScadaPageConfig
{
    public string PageName { get; set; } = "Main Plant";
    public double CanvasWidth { get; set; } = 1500;
    public double CanvasHeight { get; set; } = 800;
    public List<ScadaWidgetConfig> Widgets { get; set; } = new();
}

public sealed class ScadaWidgetConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public ScadaWidgetType Type { get; set; } = ScadaWidgetType.Label;
    public string Caption { get; set; } = "New object";
    public double X { get; set; } = 80;
    public double Y { get; set; } = 80;
    public double Width { get; set; } = 220;
    public double Height { get; set; } = 60;
    public double Rotation { get; set; }
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
    public bool IsMomentary { get; set; }
    public double Minimum { get; set; }
    public double Maximum { get; set; } = 100;
}

