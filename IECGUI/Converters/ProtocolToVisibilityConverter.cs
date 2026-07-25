using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using IEC.Shared.Models;

namespace IECGUI.Converters
{
    public class ProtocolToVisibilityConverter : IValueConverter
    {
        // Which protocol should return Visible (e.g. ModbusTcp)
        public ProtocolsType TargetProtocol { get; set; }

        // If true, visibility is inverted
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;

            ProtocolsType protocol;
            if (value is ProtocolsType p)
            {
                protocol = p;
            }
            else if (value is string s && Enum.TryParse<ProtocolsType>(s, true, out var parsed))
            {
                protocol = parsed;
            }
            else
            {
                return Visibility.Collapsed;
            }

            bool isMatch = protocol == TargetProtocol;
            if (Invert) isMatch = !isMatch;
            return isMatch ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}