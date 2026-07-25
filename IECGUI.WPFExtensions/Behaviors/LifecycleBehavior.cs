using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using Microsoft.Xaml.Behaviors; 

namespace IECGUI.Common.WPFExtensions.Behaviors
{
    public class LifecycleBehavior : Behavior<UserControl>
    {
        
        public static readonly DependencyProperty LoadedCommandProperty =
            DependencyProperty.Register("LoadedCommand", typeof(ICommand), typeof(LifecycleBehavior), new PropertyMetadata(null));

        public ICommand LoadedCommand
        {
            get { return (ICommand)GetValue(LoadedCommandProperty); }
            set { SetValue(LoadedCommandProperty, value); }
        }

        
        public static readonly DependencyProperty UnloadedCommandProperty =
            DependencyProperty.Register("UnloadedCommand", typeof(ICommand), typeof(LifecycleBehavior), new PropertyMetadata(null));

        public ICommand UnloadedCommand
        {
            get { return (ICommand)GetValue(UnloadedCommandProperty); }
            set { SetValue(UnloadedCommandProperty, value); }
        }


        protected override void OnAttached()
        {
            base.OnAttached();
          
            AssociatedObject.Loaded += OnControlLoaded;
            AssociatedObject.Unloaded += OnControlUnloaded;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
          
            AssociatedObject.Loaded -= OnControlLoaded;
            AssociatedObject.Unloaded -= OnControlUnloaded;
        }

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
           
            if (LoadedCommand != null && LoadedCommand.CanExecute(null))
            {
                LoadedCommand.Execute(null);
            }
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
          
            if (UnloadedCommand != null && UnloadedCommand.CanExecute(null))
            {
                UnloadedCommand.Execute(null);
            }
        }
    }
}