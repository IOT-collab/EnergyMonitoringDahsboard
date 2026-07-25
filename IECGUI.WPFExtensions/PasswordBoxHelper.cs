using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace IECGUI.Common.WPFExtensions
{
    public static class FocusExtension
    {
        // 1. Define the Attached Property "IsFocused"
        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached(
                "IsFocused",
                typeof(bool),
                typeof(FocusExtension),
                new UIPropertyMetadata(false, OnIsFocusedPropertyChanged));

        // 2. Getters and Setters
        public static bool GetIsFocused(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsFocusedProperty);
        }

        public static void SetIsFocused(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFocusedProperty, value);
        }

        // 3. The Logic (What happens when the property changes)
        private static void OnIsFocusedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Control control && (bool)e.NewValue)
            {
                // Hook into the Loaded event to ensure visual tree is ready
                control.Loaded += (sender, args) =>
                {
                    // Use Dispatcher to wait for rendering to finish (vital for Viewbox/Shadows)
                    control.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                    {
                        control.Focus();
                        System.Windows.Input.Keyboard.Focus(control);
                    }));
                };

                // If the control is already loaded, trigger immediately
                if (control.IsLoaded)
                {
                    control.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                    {
                        control.Focus();
                        System.Windows.Input.Keyboard.Focus(control);
                    }));
                }
            }
        }
    }



    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty BindablePasswordProperty =
            DependencyProperty.RegisterAttached(
                "BindablePassword",
                typeof(string),
                typeof(PasswordBoxHelper),
                new PropertyMetadata("", OnBindablePasswordChanged));

        public static string GetBindablePassword(DependencyObject obj)
        {
            return (string)obj.GetValue(BindablePasswordProperty);
        }

        public static void SetBindablePassword(DependencyObject obj, string value)
        {
            obj.SetValue(BindablePasswordProperty, value);
        }

        private static void OnBindablePasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = d as PasswordBox;
            box.PasswordChanged -= PasswordChanged;

            if (!GetIsUpdating(box))
            {
                box.Password = (string)e.NewValue;
            }

            box.PasswordChanged += PasswordChanged;
        }

        public static readonly DependencyProperty BindPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BindPassword",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnBindPasswordChanged));

        public static bool GetBindPassword(DependencyObject obj)
        {
            return (bool)obj.GetValue(BindPasswordProperty);
        }

        public static void SetBindPassword(DependencyObject obj, bool value)
        {
            obj.SetValue(BindPasswordProperty, value);
        }

        private static void OnBindPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = d as PasswordBox;

            if ((bool)e.OldValue)
            {
                box.PasswordChanged -= PasswordChanged;
            }

            if ((bool)e.NewValue)
            {
                box.PasswordChanged += PasswordChanged;
            }
        }

        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached(
                "IsUpdating",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false));

        private static bool GetIsUpdating(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsUpdatingProperty);
        }

        private static void SetIsUpdating(DependencyObject obj, bool value)
        {
            obj.SetValue(IsUpdatingProperty, value);
        }

        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            var box = sender as PasswordBox;
            SetIsUpdating(box, true);
            SetBindablePassword(box, box.Password);
            SetIsUpdating(box, false);
        }
    }


}
