using Microsoft.Xaml.Behaviors; // Requires Microsoft.Xaml.Behaviors.Wpf NuGet
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;

namespace IECGUI.Common.WPFExtensions
{
    // This reduces your XAML button code by 90%
    public class MomentaryBehavior : Behavior<UIElement>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(MomentaryBehavior));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(MomentaryBehavior));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseLeftButtonDown += OnDown;
            AssociatedObject.PreviewMouseLeftButtonUp += OnUp;
            AssociatedObject.MouseLeave += OnUp; // Safety
            AssociatedObject.PreviewTouchDown += OnDown;
            AssociatedObject.PreviewTouchUp += OnUp;
            AssociatedObject.LostMouseCapture += OnUp;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseLeftButtonDown -= OnDown;
            AssociatedObject.PreviewMouseLeftButtonUp -= OnUp;
            AssociatedObject.MouseLeave -= OnUp;
            AssociatedObject.PreviewTouchDown -= OnDown;
            AssociatedObject.PreviewTouchUp -= OnUp;
            AssociatedObject.LostMouseCapture -= OnUp;
        }

        private void OnDown(object sender, RoutedEventArgs e) => Execute(true);
        private void OnUp(object sender, RoutedEventArgs e) => Execute(false);

        private void Execute(bool isPressed)
        {
            // We pass the parameter AND the state (Pressed/Released)
            if (Command != null && Command.CanExecute(null))
            {
                // Format: "Parameter|True" or "Parameter|False"
                string param = $"{CommandParameter}|{isPressed}";
                Command.Execute(param);
            }
        }
    }

    public class BoolToColorConverter : MarkupExtension, IValueConverter
    {
        public object True { get; set; }
        public object False { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            object colorValue = flag ? True : False;

            // Convert string hex codes (e.g., "#0F9D58") to Color objects
            if (colorValue is string colorString)
            {
                return ColorConverter.ConvertFromString(colorString);
            }
            return colorValue; // Return as is if already a Color
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
