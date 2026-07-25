using System.Windows;
using System.Windows.Input;

namespace IECGUI.Common.WPFExtensions.Behaviors
{
    public static class MouseBehavior
    {
        // 1. Mouse Down Command
        public static readonly DependencyProperty MouseDownCommandProperty =
            DependencyProperty.RegisterAttached("MouseDownCommand", typeof(ICommand), typeof(MouseBehavior), new PropertyMetadata(null, OnMouseDownCommandChanged));

        public static ICommand GetMouseDownCommand(DependencyObject obj) => (ICommand)obj.GetValue(MouseDownCommandProperty);
        public static void SetMouseDownCommand(DependencyObject obj, ICommand value) => obj.SetValue(MouseDownCommandProperty, value);

        private static void OnMouseDownCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                element.PreviewMouseLeftButtonDown -= Element_PreviewMouseLeftButtonDown;
                if (e.NewValue != null) element.PreviewMouseLeftButtonDown += Element_PreviewMouseLeftButtonDown;
            }
        }

        private static void Element_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DependencyObject d)
            {
                ICommand command = GetMouseDownCommand(d);
                object param = GetCommandParameter(d);
                if (command != null && command.CanExecute(param)) command.Execute(param);
            }
        }

        // 2. Mouse Up Command
        public static readonly DependencyProperty MouseUpCommandProperty =
            DependencyProperty.RegisterAttached("MouseUpCommand", typeof(ICommand), typeof(MouseBehavior), new PropertyMetadata(null, OnMouseUpCommandChanged));

        public static ICommand GetMouseUpCommand(DependencyObject obj) => (ICommand)obj.GetValue(MouseUpCommandProperty);
        public static void SetMouseUpCommand(DependencyObject obj, ICommand value) => obj.SetValue(MouseUpCommandProperty, value);

        private static void OnMouseUpCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                element.PreviewMouseLeftButtonUp -= Element_PreviewMouseLeftButtonUp;
                if (e.NewValue != null) element.PreviewMouseLeftButtonUp += Element_PreviewMouseLeftButtonUp;
            }
        }

        private static void Element_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is DependencyObject d)
            {
                ICommand command = GetMouseUpCommand(d);
                object param = GetCommandParameter(d);
                if (command != null && command.CanExecute(param)) command.Execute(param);
            }
        }

        // 3. Command Parameter (Shared)
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached("CommandParameter", typeof(object), typeof(MouseBehavior), new PropertyMetadata(null));

        public static object GetCommandParameter(DependencyObject obj) => obj.GetValue(CommandParameterProperty);
        public static void SetCommandParameter(DependencyObject obj, object value) => obj.SetValue(CommandParameterProperty, value);
    }
}