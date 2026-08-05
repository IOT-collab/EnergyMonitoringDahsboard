using IECGUI.Services;
using IECGUI.View;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualBasic;

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
        public string Password { get => _password; set => SetProperty(ref _password, value); }
        private string _password;

        private readonly IDialogService _dialogService;


        private readonly INavigationService _navigation;
        public HomePageViewModel(INavigationService navigation , IDialogService dialogService)
        {
            _navigation = navigation;
            _dialogService = dialogService;

            SldViewCommand = new RelayCommand(SLDViewLogin);
            EnergyViewCommand = new RelayCommand(() => _navigation.NavigateTo<EnergyMonitorViewModel>()); //_navigation.NavigateTo(new Dashboard1ViewModel(_navigation));
            GaugeViewCommand = new RelayCommand(() => _navigation.NavigateTo<EnergyMonitorViewModel2>());
            ConfigViewCommand = new RelayCommand(() => _navigation.NavigateTo<ConfigurationViewModel>());
            ProtRelayMonitorViewCommand = new RelayCommand(() => _navigation.NavigateTo<Iec61850MonitorViewModel>());
            MqttViewCommad = new RelayCommand(() => _navigation.NavigateTo<MqttMonitorViewModel>());
            ReportViewerCommand = new RelayCommand(() => _navigation.NavigateTo<ReportViewerViewModel>());
            AlarmViewCommand = new RelayCommand(() => _navigation.NavigateTo<EnergyMonitorViewModel2>());
        }

        private void SLDViewLogin()
        {
            _navigation.NavigateTo<Dashboard1ViewModel>();

            // string name = Interaction.InputBox("Please enter the Password", "Password Required For SLD Operation",  "");


            //if (!string.IsNullOrWhiteSpace(name))
            //{
            //    if (name == "1234") { _navigation.NavigateTo<Dashboard1ViewModel>(); } else { _dialogService.ShowWarning("Wrong Password"); return; }
            //}


        }
    }
}
