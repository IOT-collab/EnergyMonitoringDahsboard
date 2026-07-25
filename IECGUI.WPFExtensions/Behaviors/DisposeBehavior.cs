using System;
using System.Windows;

namespace IECGUI.Common.WPFExtensions
{
    public static class DisposeBehavior
    {
        // 1. Register the Attached Property "AutoDispose"
        public static readonly DependencyProperty AutoDisposeProperty =
            DependencyProperty.RegisterAttached(
                "AutoDispose",
                typeof(bool),
                typeof(DisposeBehavior),
                new PropertyMetadata(false, OnAutoDisposeChanged));

        // Helper Get/Set methods required for XAML
        public static bool GetAutoDispose(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoDisposeProperty);
        }

        public static void SetAutoDispose(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoDisposeProperty, value);
        }

        // 2. Handle Property Changes
        private static void OnAutoDisposeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                // If set to True, subscribe. If False, unsubscribe.
                if ((bool)e.NewValue)
                {
                    element.Unloaded += Element_Unloaded;
                }
                else
                {
                    element.Unloaded -= Element_Unloaded;
                }
            }
        }

        // 3. The Logic (What used to be in your Code Behind)
        private static void Element_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                // Check if DataContext implements IDisposable
                if (element.DataContext is IDisposable disposableViewModel)
                {
                    disposableViewModel.Dispose();

                    // Optional: Debug log to verify it's working
                    // System.Diagnostics.Debug.WriteLine($"Disposed ViewModel for {element.GetType().Name}");
                }

                // Optional: Cleanup event subscription to prevent memory leaks 
                // (though typically Unloaded implies the view is going away)
                element.Unloaded -= Element_Unloaded;
            }
        }
    }
}