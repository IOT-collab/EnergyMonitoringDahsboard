using IEC.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace IECGUI.View;

public partial class IndustrialSldDefinitionDialog : Window
{
    public IndustrialSldDefinition? Definition { get; private set; }

    public IndustrialSldDefinitionDialog()
    {
        InitializeComponent();
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void CreateClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(IncomerCountBox.Text, out var incomers)) incomers = 2;
        if (!int.TryParse(CouplerCountBox.Text, out var couplers)) couplers = 1;
        var outgoingText = OutgoingNamesBox.Text ?? string.Empty;
        if (!int.TryParse(OutgoingCountBox.Text, out var outgoings)) outgoings = ParseNames(outgoingText).Count;
        outgoings = Math.Clamp(Math.Max(1, outgoings), 1, 24);
        incomers = Math.Clamp(incomers, 1, 8);
        couplers = Math.Clamp(couplers, 0, 7);
        var voltage = (VoltageBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "MV";
        Definition = new IndustrialSldDefinition
        {
            Name = string.IsNullOrWhiteSpace(NameBox.Text) ? $"{voltage} Substation SLD" : NameBox.Text.Trim(),
            VoltageLevel = voltage,
            IncomerNames = NormalizeNames(IncomerNamesBox.Text, incomers, "INCOMER"),
            BusCouplerNames = NormalizeNames(CouplerNamesBox.Text, couplers, "BUS COUPLER"),
            OutgoingNames = NormalizeNames(outgoingText, outgoings, "OUTGOING")
        };
        DialogResult = true;
    }

    private static List<string> NormalizeNames(string? text, int count, string prefix)
    {
        var names = ParseNames(text);
        while (names.Count < count) names.Add($"{prefix}-{names.Count + 1}");
        return names.Take(count).ToList();
    }

    private static List<string> ParseNames(string? text)
        => (text ?? string.Empty).Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
}