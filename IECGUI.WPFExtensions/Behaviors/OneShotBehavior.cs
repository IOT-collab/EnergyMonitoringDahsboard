using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors; // NuGet: Microsoft.Xaml.Behaviors.Wpf

namespace IECGUI.Common.WPFExtensions.Behaviors
{
    public class OneShotBehavior : Behavior<ButtonBase>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(OneShotBehavior));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(OneShotBehavior));

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            // We only care about the "Down" events to trigger the action
            AssociatedObject.PreviewMouseLeftButtonDown += OnPress;
            AssociatedObject.PreviewTouchDown += OnTouchDown;
            
            // We still listen to Up just to release the UI "Capture" (visuals), 
            // but we won't send any command.
            AssociatedObject.PreviewTouchUp += OnTouchUp;
            AssociatedObject.PreviewMouseLeftButtonUp += OnRelease;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseLeftButtonDown -= OnPress;
            AssociatedObject.PreviewTouchDown -= OnTouchDown;
            AssociatedObject.PreviewTouchUp -= OnTouchUp;
            AssociatedObject.PreviewMouseLeftButtonUp -= OnRelease;
        }

        private void OnTouchDown(object sender, TouchEventArgs e)
        {
            if (!AssociatedObject.IsEnabled) return;
            
            // Capture touch so the button looks pressed visually while holding
            AssociatedObject.CaptureTouch(e.TouchDevice);
            
            // Fire logic
            OnPress(sender, null);
        }

        private void OnTouchUp(object sender, TouchEventArgs e)
        {
            // Just release the visual capture. DO NOT SEND COMMAND.
            AssociatedObject.ReleaseTouchCapture(e.TouchDevice);
        }

        private void OnPress(object sender, RoutedEventArgs e)
        {
            if (!AssociatedObject.IsEnabled) return;

            // Send "True" (1) immediately
            ExecuteCommand(true);
        }

        private void OnRelease(object sender, RoutedEventArgs e)
        {
            // Do nothing. 
            // We do NOT send "False" because the PLC resets the tag itself.
        }

        private void ExecuteCommand(bool isPressed)
        {
            if (Command == null) return;

            // We construct the string "Parameter|True"
            // We never send "|False"
            string param = $"{CommandParameter}|{isPressed}";

            if (Command.CanExecute(param))
            {
                Command.Execute(param);
            }
        }
    }
}