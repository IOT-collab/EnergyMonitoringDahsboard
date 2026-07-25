using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System;
using System.Windows;


namespace IECGUI.Common.WPFExtensions.Convertors
{
    public class OeeToGeometryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percentage = 0;
            if (value is double d) percentage = d;
            else if (value is int i) percentage = i;
            else if (value is string s && double.TryParse(s, out double parsed)) percentage = parsed;

            // Clamp percentage between 0 and 100
            if (percentage < 0) percentage = 0;
            if (percentage > 100) percentage = 100;

            // If 0, return start point only
            if (percentage <= 0.01)
            {
                return Geometry.Parse("M 100,10");
            }

            // Calculate angle (0% = 0 deg, 100% = 360 deg)
            double angle = (percentage / 100.0) * 360.0;

            // Handle full circle case (ArcTo can behave unexpectedly with full 360)
            if (angle >= 360) angle = 359.99;

            // Convert to Radians. Subtract 90 degrees to start from top (12 o'clock).
            double radians = (angle - 90) * (Math.PI / 180.0);

            // Center (100, 100), Radius 90
            double radius = 90;
            double centerX = 100;
            double centerY = 100;

            // Calculate End Point
            double endX = centerX + radius * Math.Cos(radians);
            double endY = centerY + radius * Math.Sin(radians);

            bool isLargeArc = angle > 180.0;

            // Create Geometry String
            // M [StartPoint] A [Radius] 0 [IsLargeArc] [SweepDirection] [EndPoint]
            // Start Point is fixed at top: (100, 10)
            string geometryString = string.Format(CultureInfo.InvariantCulture,
                "M 100,10 A {0},{0} 0 {1} 1 {2},{3}",
                radius,
                isLargeArc ? 1 : 0,
                endX,
                endY);

            return Geometry.Parse(geometryString);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class OeeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double oee = 0;

            // Safe casting
            if (value is double d) oee = d;
            else if (value is int i) oee = i;
            else if (value is string s && double.TryParse(s, out double res)) oee = res;

            // Logic: If < 80, return Red. Else return Green Gradient.
            if (oee < 80)
            {
                // Red Color (Danger)
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
            }
            else
            {
                // Original Green Gradient
                return new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString("#4CAF50"),
                    (Color)ColorConverter.ConvertFromString("#8BC34A"),
                    new Point(0, 0),
                    new Point(1, 0));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
