using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace IECGUI.Common.WPFExtensions.Convertors
{
    public class BoolToStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Brushes.LightGray;

            bool isOn = false;

            if (value is bool b) isOn = b;
            else if (value is int i) isOn = i > 0;
            else if (value is string s)
            {
                if (bool.TryParse(s, out bool res)) isOn = res;
                else if (int.TryParse(s, out int ires)) isOn = ires > 0;
            }

            // Green for ON, Red (or dark gray) for OFF
            return isOn ? new SolidColorBrush(Color.FromRgb(40, 167, 69)) : new SolidColorBrush(Color.FromRgb(220, 53, 69));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
