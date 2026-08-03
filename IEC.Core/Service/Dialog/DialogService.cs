using IECGUI.Services;
using IECGUI.ViewModel;
using IECGUI.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IEC.Core.Service.Dialog
{
    public class DialogService : IDialogService
    {
        public bool ShowYesNo(string message, string title = "Confirm")
        {
            // TRUE = Confirmation (Red button, Show Cancel)
            var vm = new CustomMessageBoxViewModel(message, title, "Yes", "Cancel", true);
            return ShowWindow(vm);
        }

        public void ShowMessage(string message, string DialogBoxName = "Information")
        {
            // FALSE = Info (Blue button, Hide Cancel)
            var vm = new CustomMessageBoxViewModel(message, DialogBoxName, "OK", "", false);
            ShowWindow(vm);
        }

        public void ShowWarning(string message)
        {
            // FALSE = Info (Blue button, Hide Cancel)
            var vm = new CustomMessageBoxViewModel(message, "Warning", "OK", "", false);
            ShowWindow(vm);
        }



        // Helper method to reduce code duplication
        private bool ShowWindow(CustomMessageBoxViewModel vm)
        {
            var msgBox = new CustomMessageBox();
            if (Application.Current.MainWindow != null)
            {
                msgBox.Owner = Application.Current.MainWindow;
            }
            msgBox.Initialize(vm);
            var result = msgBox.ShowDialog();
            return result == true;
        }

    }

}
