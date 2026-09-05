using IEC.Shared.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace IECGUI.Converters
{
    /// <summary>Shows a section when the selected protocol is one of a comma-separated set.</summary>
    public sealed class ProtocolSetToVisibilityConverter : IValueConverter
    {
        public string Protocols { get; set; } = string.Empty;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ProtocolsType selected)
                return Visibility.Collapsed;

            var allowed = Protocols.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Enum.TryParse<ProtocolsType>(x.Trim(), true, out var p) ? p : (ProtocolsType?)null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value);
            return allowed.Contains(selected) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
