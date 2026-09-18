using IEC.Shared.Models;
using System;
using System.Collections.Generic;

namespace IECGUI.Services;

/// <summary>Creates editable LV/MV/HV substation SLDs and evaluates incomer/coupler interlocking.</summary>
public static class IndustrialSldTemplateService
{
    public static int MaximumClosedBreakers(int incomers, int busCouplers) => Math.Max(0, busCouplers) + 1;

    public static string LogicSummary(int incomers, int busCouplers)
    {
        var n = Math.Max(1, incomers);
        var m = Math.Max(0, busCouplers);
        return $"{m + 1}-out-of-{n + m} (closed <= {MaximumClosedBreakers(n, m)})";
    }

    public static bool IsPermittedState(IReadOnlyList<bool> incomersClosed, IReadOnlyList<bool> couplersClosed)
    {
        var closed = 0;
        for (var i = 0; i < incomersClosed.Count; i++) if (incomersClosed[i]) closed++;
        for (var i = 0; i < couplersClosed.Count; i++) if (couplersClosed[i]) closed++;
        return closed <= MaximumClosedBreakers(incomersClosed.Count, couplersClosed.Count);
    }

    public static ScadaPageConfig CreateTemplate(
        string voltageLevel,
        int incomers,
        int busCouplers,
        int outgoings,
        string? deviceName,
        IReadOnlyList<string>? incomerNames = null,
        IReadOnlyList<string>? busCouplerNames = null,
        IReadOnlyList<string>? outgoingNames = null)
    {
        var level = string.IsNullOrWhiteSpace(voltageLevel) ? "MV" : voltageLevel.Trim().ToUpperInvariant();
        var n = Math.Clamp(incomers, 1, 8);
        var m = Math.Clamp(busCouplers, 0, 7);
        var feeders = Math.Clamp(outgoings, 1, 24);
        var page = new ScadaPageConfig
        {
            PageName = $"{level} Substation SLD",
            CanvasWidth = 1500,
            CanvasHeight = 800
        };

        Add(page, ScadaWidgetType.Label, $"{level} SUBSTATION SINGLE-LINE DIAGRAM", 35, 20, 700, 48, deviceName, null, "#FFD27A", "#102A3E");
        Add(page, ScadaWidgetType.Label, $"Interlock: {LogicSummary(n, m)}", 1030, 25, 420, 38, deviceName, null, "#9EDFF2", "#102A3E");

        const double busY = 365;
        const double busX = 120;
        const double busWidth = 1260;
        var sections = m + 1;
        var sectionWidth = busWidth / sections;
        for (var section = 0; section < sections; section++)
        {
            var x = busX + section * sectionWidth;
            Add(page, ScadaWidgetType.Line, $"BUS {section + 1}", x, busY, sectionWidth - 8, 1, deviceName, null, "#E6F8FF", "#19364A", lineThickness: 6);
        }

        for (var i = 0; i < n; i++)
        {
            var x = busX + (i + 0.5) * busWidth / n;
            var name = NameAt(incomerNames, i, $"INCOMER-{i + 1}");
            Add(page, ScadaWidgetType.Label, name, x - 75, 100, 150, 32, deviceName, null, "#E6F8FF", "#19364A");
            Add(page, ScadaWidgetType.Breaker, name, x - 35, 225, 70, 88, deviceName, name, "#FFFFFF", "#19364A", true, "Incomer");
            Add(page, ScadaWidgetType.Line, string.Empty, x - 0.5, 175, 1, 50, deviceName, null, "#FFFFFF", "#19364A", lineThickness: 4);
            Add(page, ScadaWidgetType.Line, string.Empty, x - 0.5, 313, 1, busY - 313, deviceName, null, "#FFFFFF", "#19364A", lineThickness: 4);
        }

        for (var c = 0; c < m; c++)
        {
            var x = busX + (c + 1) * sectionWidth;
            var name = NameAt(busCouplerNames, c, $"BUS COUPLER-{c + 1}");
            Add(page, ScadaWidgetType.Breaker, name, x - 38, busY - 42, 76, 94, deviceName, name, "#FFFFFF", "#19364A", true, "BusCoupler");
        }

        for (var i = 0; i < feeders; i++)
        {
            var x = busX + (i + 0.5) * busWidth / feeders;
            var name = NameAt(outgoingNames, i, $"OUTGOING-{i + 1}");
            Add(page, ScadaWidgetType.Line, string.Empty, x - 0.5, busY + 10, 1, 62, deviceName, null, "#FFFFFF", "#19364A", lineThickness: 4);
            Add(page, ScadaWidgetType.Breaker, name, x - 38, busY + 62, 76, 94, deviceName, name, "#FFFFFF", "#19364A", true, "Outgoing");
            Add(page, ScadaWidgetType.Label, name, x - 70, busY + 165, 140, 30, deviceName, null, "#E6F8FF", "#19364A");
        }

        Add(page, ScadaWidgetType.Label, "Primary editable SLD layout — assign live tags and refine symbols after generation.", 35, 735, 900, 32, deviceName, null, "#9EDFF2", "#102A3E");
        return page;
    }

    private static string NameAt(IReadOnlyList<string>? names, int index, string fallback)
    {
        var name = names != null && index >= 0 && index < names.Count ? names[index] : null;
        return string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
    }

    private static void Add(ScadaPageConfig page, ScadaWidgetType type, string caption, double x, double y, double width, double height,
        string? device, string? parameter, string foreground, string background, bool dynamic = false, string symbolKind = "", double lineThickness = 3)
    {
        page.Widgets.Add(new ScadaWidgetConfig
        {
            Type = type,
            Caption = caption,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            LineThickness = lineThickness,
            DeviceName = device,
            ParameterName = parameter,
            Foreground = foreground,
            Background = background,
            DynamicStateColors = dynamic,
            SymbolKind = symbolKind,
            OnForeground = "#FFFFFF",
            OnBackground = "#18A957",
            OffForeground = "#FFFFFF",
            OffBackground = "#5A2A35"
        });
    }
}
