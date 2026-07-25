using IPCSoftware.UI.CommonViews.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace IPCSoftware.Common.WPFExtensions.Convertors
{
    public class SingleBtnEqualityConverter : IMultiValueConverter
    {


        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return false;
            return Equals(values[0], values[1]);
        }

        

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
