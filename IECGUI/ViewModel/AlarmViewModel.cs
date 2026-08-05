using IECGUI.Services;
using System.Windows.Input;

namespace IECGUI.ViewModel
{
    public class AlarmViewModel : BaseViewModel
    {
        private readonly INavigationService _navigation;

        public AlarmMonitoringService AlarmService { get; }
        public ICommand BackCommand { get; }

        public AlarmViewModel(INavigationService navigation, AlarmMonitoringService alarmService)
        {
            _navigation = navigation;
            AlarmService = alarmService;
            BackCommand = new RelayCommand(() => _navigation.NavigateTo<HomePageViewModel>());
        }
    }
}
