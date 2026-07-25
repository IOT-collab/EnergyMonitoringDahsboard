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
    public class OnOffToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() == "ON")
                return new SolidColorBrush(Color.FromRgb(200, 255, 200)); // light green

            return new SolidColorBrush(Color.FromRgb(255, 200, 200)); // light red
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
