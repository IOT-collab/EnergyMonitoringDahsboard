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
    public class ResultToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string result = value.ToString();

            return result switch
            {
                "OK" => new SolidColorBrush(Colors.LimeGreen),
                "NG" => new SolidColorBrush(Colors.Red),
                "Unchecked" => new SolidColorBrush(Colors.Gray),
                _ => new SolidColorBrush(Colors.Gray),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class MsToSecondsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0.0;

            // Attempt to convert input to double (handles int, double, string)
            if (double.TryParse(value.ToString(), out double milliseconds))
            {
                // Divide by 1000 to get seconds
                return milliseconds / 100.0;
            }

            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
