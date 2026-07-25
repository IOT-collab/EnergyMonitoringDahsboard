using Microsoft.Xaml.Behaviors;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace IECGUI.Common.WPFExtensions
{
    public class ImageClickBehavior : Behavior<System.Windows.Controls.Image>
    {
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ImageClickBehavior));

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseDown += ImageClicked;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseDown -= ImageClicked;
        }

        private void ImageClicked(object sender, MouseButtonEventArgs e)
        {
            if (Command?.CanExecute(AssociatedObject.DataContext) == true)
                Command.Execute(AssociatedObject.DataContext);
        }
    }

    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue) return !booleanValue;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
