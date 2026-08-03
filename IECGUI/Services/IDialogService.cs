using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IECGUI.Services
{
    public interface IDialogService
    {
        void ShowMessage(string message, string DialogBoxName = "Information");
        void ShowWarning(string message);

        bool ShowYesNo(string message, string title = "Confirm");



    }

}
