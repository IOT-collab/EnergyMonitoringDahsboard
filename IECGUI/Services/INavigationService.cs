using IECGUI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IECGUI.Services
{
    public interface INavigationService
    {
        object CurrentView { get; }
        event Action CurrentViewChanged;
        void NavigateTo<T>() where T : BaseViewModel;
    }
}
