using IECGUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IECGUI.ViewModel
{
    public class RelayCardViewModel : BaseViewModel
    {
        private Window _window;
        public ICommand CloseCard { get; set; }

        private readonly INavigationService _navigation;
        public RelayCardViewModel(Window window, INavigationService navigation)
        {
            _window = window;
            CloseCard = new RelayCommand(ExecuteCloseWindow);
            _navigation = navigation;
        }

        private void ExecuteCloseWindow()
        {
           _window.Close();
        }

    }
}
