using IECGUI.ViewModel;
using System.Windows;

namespace IECGUI.View
{
    public partial class CustomMessageBox : Window
    {
        public CustomMessageBox()
        {
            InitializeComponent();
        }

        // Method to inject ViewModel and subscribe to events
        public void Initialize(CustomMessageBoxViewModel vm)
        {
            this.DataContext = vm;

            // Subscribe to the CloseRequested event
            vm.CloseRequested += (result) =>
            {
                this.DialogResult = result;
                this.Close();
            };
        }
    }
}