using System.Windows;
using System.Windows.Controls;

namespace IECGUI.Common.WPFExtensions
{
    public static class BlockTabChangeBehavior
    {
        public static readonly DependencyProperty BlockWhenProperty =
        DependencyProperty.RegisterAttached(
            "BlockWhen",
            typeof(bool),
            typeof(BlockTabChangeBehavior),
            new PropertyMetadata(false));

        public static bool GetBlockWhen(DependencyObject obj)
            => (bool)obj.GetValue(BlockWhenProperty);

        public static void SetBlockWhen(DependencyObject obj, bool value)
            => obj.SetValue(BlockWhenProperty, value);

        private static readonly DependencyProperty LastValidIndexProperty =
            DependencyProperty.RegisterAttached(
                "LastValidIndex",
                typeof(int),
                typeof(BlockTabChangeBehavior),
                new PropertyMetadata(0));

        private static int GetLastValidIndex(DependencyObject obj)
            => (int)obj.GetValue(LastValidIndexProperty);

        private static void SetLastValidIndex(DependencyObject obj, int value)
            => obj.SetValue(LastValidIndexProperty, value);

        static BlockTabChangeBehavior()
        {
            EventManager.RegisterClassHandler(
                typeof(TabControl),
                TabControl.SelectionChangedEvent,
                new SelectionChangedEventHandler(OnSelectionChanged));
        }

        private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not TabControl tab || e.Source != tab)
                return;

            int newIndex = tab.SelectedIndex;
            int lastIndex = GetLastValidIndex(tab);

            if (newIndex == lastIndex)
                return;

            if (GetBlockWhen(tab))
            {
                // Show warning using standard MessageBox (no dependency on UI.CommonViews DialogService)
                MessageBox.Show(
                    "Unsaved changes detected.\n\nYou must SAVE your changes before switching tabs.",
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // Revert tab selection
                tab.Dispatcher.BeginInvoke(() =>
                {
                    tab.SelectedIndex = lastIndex;
                });
            }
            else
            {
                // Allow change
                SetLastValidIndex(tab, newIndex);
            }
        }
    }
}
