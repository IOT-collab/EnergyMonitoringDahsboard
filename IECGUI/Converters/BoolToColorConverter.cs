using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IECGUI.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isOnline = value is bool b && b;
            return isOnline
                ? new SolidColorBrush(Color.FromRgb(39, 174, 96))   // Green
                : new SolidColorBrush(Color.FromRgb(192, 57, 43));  // Red
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}