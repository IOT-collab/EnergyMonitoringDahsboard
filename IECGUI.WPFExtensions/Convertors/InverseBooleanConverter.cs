using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace IECGUI.Common.WPFExtensions.Convertors
{
    public class InverseBooleanConverter : IValueConverter
    {
        // Value = input boolean from the ViewModel (e.g., IsEnabled)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                // Return the opposite of the input boolean
                return !booleanValue; // Logic tested: NOT 



            }
            // If input is null or not a boolean, return the value unchanged 
            // (or handle with a safe default, depending on requirements)
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Re-inverts the boolean for two-way binding.
            if (value is bool booleanValue)
            {
                return !booleanValue;
            }
            return value;
        }
    }
}
