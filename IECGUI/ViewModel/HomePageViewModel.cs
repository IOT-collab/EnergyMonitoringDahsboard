using IECGUI.Services;
using IECGUI.View;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IECGUI.ViewModel
{
    public class HomePageViewModel :BaseViewModel
    {
        public ICommand SldViewCommand { get; }
        public ICommand EnergyViewCommand { get; }
        public ICommand GaugeViewCommand { get; }
        public ICommand ConfigViewCommand { get; }
        public ICommand ProtRelayMonitorViewCommand { get; }

        public ICommand MqttViewCommad { get; }

        public ICommand ReportViewerCommand { get; }

        public ICommand AlarmViewCommand { get; }



        private readonly INavigationService _navigation;
        public HomePageViewModel(INavigationService navigation)
        {
            _navigation = navigation;


            SldViewCommand = new RelayCommand(() => _navigation.NavigateTo<Dashboard1ViewModel>());
            EnergyViewCommand = new RelayCommand(() => _navigation.NavigateTo<EnergyMonitorViewModel>()); //_navigation.NavigateTo(new Dashboard1ViewModel(_navigation));
            GaugeViewCommand = new RelayCommand(() => _navigation.NavigateTo<EnergyMonitorViewModel2>());
            ConfigViewCommand = new RelayCommand(() => _navigation.NavigateTo<ConfigurationViewModel>());
            ProtRelayMonitorViewCommand = new RelayCommand(() => _navigation.NavigateTo<Iec61850MonitorViewModel>());
            MqttViewCommad = new RelayCommand(() => _navigation.NavigateTo<MqttMonitorViewModel>());
            ReportViewerCommand = new RelayCommand(() => _navigation.NavigateTo<ReportViewerViewModel>());
        }
    }
}
