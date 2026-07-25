using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IECGUI.Common.WPFExtensions.Convertors
{
    public class GroupVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts group name to Visibility
        /// </summary>
        /// <param name="value">The group name from the button (e.g., "Tray Lift")</param>
        /// <param name="targetType">Target type (Visibility)</param>
        /// <param name="parameter">The target group name to match against (e.g., "Tray Lift")</param>
        /// <param name="culture">Culture info</param>
        /// <returns>Visible if group matches, Collapsed otherwise</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string? itemGroup = value.ToString();
            string? targetGroup = parameter.ToString();

            return itemGroup == targetGroup ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}