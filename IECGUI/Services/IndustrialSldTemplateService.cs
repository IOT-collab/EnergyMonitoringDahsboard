using IEC.Shared.Models;
using System;
using System.Collections.Generic;

namespace IECGUI.Services;

/// <summary>Creates an editable first-pass LV/MV/HV substation SLD and evaluates incomer/coupler interlocking.</summary>
public static class IndustrialSldTemplateService
{
    public static int MaximumClosedBreakers(int incomers, int busCouplers)
        => Math.Max(0, busCouplers) + 1;

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

    public static ScadaPageConfig CreateTemplate(string voltageLevel, int incomers, int busCouplers, int outgoings, string? deviceName)
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
            Add(page, ScadaWidgetType.Rectangle, $"BUS {section + 1}", x, busY, sectionWidth - 8, 10, deviceName, null, "#E6F8FF", "#19364A");
        }

        for (var i = 0; i < n; i++)
        {
            var x = busX + (i + 0.5) * busWidth / n;
            Add(page, ScadaWidgetType.Label, $"INCOMER-{i + 1}", x - 65, 100, 130, 32, deviceName, null, "#E6F8FF", "#19364A");
            Add(page, ScadaWidgetType.Rectangle, $"I{i + 1}", x - 35, 245, 70, 48, deviceName, $"I{i + 1}", "#FFFFFF", "#19364A", true);
            Add(page, ScadaWidgetType.Rectangle, string.Empty, x - 3, 175, 6, 70, deviceName, null, "#FFFFFF", "#19364A");
            Add(page, ScadaWidgetType.Rectangle, string.Empty, x - 3, 293, 6, busY - 293, deviceName, null, "#FFFFFF", "#19364A");
        }

        for (var c = 0; c < m; c++)
        {
            var x = busX + (c + 1) * sectionWidth;
            Add(page, ScadaWidgetType.Rectangle, $"BC{c + 1}", x - 38, busY - 25, 76, 60, deviceName, $"BC{c + 1}", "#FFFFFF", "#19364A", true);
        }

        for (var i = 0; i < feeders; i++)
        {
            var x = busX + (i + 0.5) * busWidth / feeders;
            Add(page, ScadaWidgetType.Rectangle, string.Empty, x - 3, busY + 10, 6, 62, deviceName, null, "#FFFFFF", "#19364A");
            Add(page, ScadaWidgetType.Rectangle, $"OUT-{i + 1}", x - 35, busY + 72, 70, 44, deviceName, $"OUT-{i + 1}", "#FFFFFF", "#19364A");
            Add(page, ScadaWidgetType.Label, $"OUTGOING-{i + 1}", x - 58, busY + 125, 116, 30, deviceName, null, "#E6F8FF", "#19364A");
        }

        Add(page, ScadaWidgetType.Label, "Primary editable SLD layout — assign live tags and refine symbols after generation.", 35, 735, 900, 32, deviceName, null, "#9EDFF2", "#102A3E");
        return page;
    }

    private static void Add(ScadaPageConfig page, ScadaWidgetType type, string caption, double x, double y, double width, double height,
        string? device, string? parameter, string foreground, string background, bool dynamic = false)
    {
        page.Widgets.Add(new ScadaWidgetConfig
        {
            Type = type,
            Caption = caption,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            DeviceName = device,
            ParameterName = parameter,
            Foreground = foreground,
            Background = background,
            DynamicStateColors = dynamic,
            OnForeground = "#FFFFFF",
            OnBackground = "#18A957",
            OffForeground = "#FFFFFF",
            OffBackground = "#5A2A35"
        });
    }
}