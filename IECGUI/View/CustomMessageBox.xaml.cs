using IECGUI.ViewModel;
using System.Windows;

namespace IECGUI.View
{
    public partial class CustomMessageBox : Window
    {
        public CustomMessageBoxResult Result { get; private set; } = CustomMessageBoxResult.Cancel;

        public CustomMessageBox()
        {
            InitializeComponent();
        }

        // Method to inject ViewModel and subscribe to events
        public void Initialize(CustomMessageBoxViewModel vm)
        {
            this.DataContext = vm;

            vm.CloseRequested += (result) =>
            {
                Complete(result ? CustomMessageBoxResult.Yes : CustomMessageBoxResult.No);
            };
            vm.ResultRequested += Complete;
        }

        private void Complete(CustomMessageBoxResult result)
        {
            Result = result;
            DialogResult = result == CustomMessageBoxResult.Yes
                ? true
                : result == CustomMessageBoxResult.No ? false : null;
            Close();
        }
    }
}
