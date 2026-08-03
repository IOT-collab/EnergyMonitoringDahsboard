using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC.Core.Service.Dialog
{
    public interface IDialogService
    {
        void ShowMessage(string message, string DialogBoxName = "Information");
        void ShowWarning(string message);

        bool ShowYesNo(string message, string title = "Confirm");



    }

}
