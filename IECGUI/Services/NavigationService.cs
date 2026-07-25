using IECGUI.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace IECGUI.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private object _currentView;

        public object CurrentView
        {
            get => _currentView;
            private set
            {
                _currentView = value;
                CurrentViewChanged?.Invoke();
            }
        }

        public event Action CurrentViewChanged;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void NavigateTo<T>() where T : BaseViewModel
        {
            CurrentView = _serviceProvider.GetRequiredService<T>();
        }
    }
}
