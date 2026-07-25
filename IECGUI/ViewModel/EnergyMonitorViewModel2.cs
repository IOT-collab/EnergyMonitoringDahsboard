using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.Services;
using IPCSoftware.Common.CommonExtensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace IECGUI.ViewModel
{
    public class EnergyMonitorViewModel2 : BaseViewModel
    {
       
        private readonly DispatcherTimer _pollTimer;

        public double VoltageA_N { get => _voltageA_N; set { _voltageA_N = value; OnPropertyChanged(); } }
        public double VoltageB_N { get => _voltageB_N; set { _voltageB_N = value; OnPropertyChanged(); } }
        public double VoltageC_N { get => _voltageC_N; set { _voltageC_N = value; OnPropertyChanged(); } }
        public double VoltageAvg { get => _voltageAvg; set { _voltageAvg = value; OnPropertyChanged(); } }

        public double CurrentA { get => _currentA; set { _currentA = value; OnPropertyChanged(); } }
        public double CurrentB { get => _currentB; set { _currentB = value; OnPropertyChanged(); } }
        public double CurrentC { get => _currentC; set { _currentC = value; OnPropertyChanged(); } }
        public double CurrentAvg { get => _currentAvg; set { _currentAvg = value; OnPropertyChanged(); } }

        public double TotalActivePower { get => _totalActivePower; set { _totalActivePower = value; OnPropertyChanged(); } }
        public double TotalReactivePower { get => _totalReactivePower; set { _totalReactivePower = value; OnPropertyChanged(); } }
        public double TotalApparentPower { get => _totalApparentPower; set { _totalApparentPower = value; OnPropertyChanged(); } }
        public double Frequency { get => _frequency; set { _frequency = value; OnPropertyChanged(); } }
        public double TotalPowerFactor { get => _totalPowerFactor; set { _totalPowerFactor = value; OnPropertyChanged(); } }

        private double _voltageA_N, _voltageB_N, _voltageC_N, _voltageAvg;
        private double _currentA, _currentB, _currentC, _currentAvg;
        private double _totalActivePower, _totalReactivePower, _totalApparentPower;
        private double _frequency, _totalPowerFactor;


        private Brush _brush1 = Brushes.Green;
        public Brush Brush1
        {   get => _brush1;
            set => SetProperty(ref _brush1, value);
        }

        private Brush _brush2 = Brushes.Green;
        public Brush Brush2
        {
            get => _brush2;
            set => SetProperty(ref _brush2, value);
        }

        public double INC1AMP
        {
            get => _inc1amp;
            set => SetProperty(ref _inc1amp, value);
        }
        private double _inc1amp;


        public double INC2AMP
        {
            get => _inc2amp;
            set => SetProperty(ref _inc2amp, value);
        }
        private double _inc2amp;


        public double INC3AMP
        {
            get => _inc3amp;
            set => SetProperty(ref _inc3amp, value);
        }
        private double _inc3amp;

        public double INC1VOLT
        {
            get => _inc1volt;
            set => SetProperty(ref _inc1volt, value);
        }
        private double _inc1volt;

        public double INC2VOLT
        {
            get => _inc2volt;
            set => SetProperty(ref _inc2volt, value);
        }
        private double _inc2volt;


        public double INC3VOLT
        {
            get => _inc3volt;
            set => SetProperty(ref _inc3volt, value);
        }
        private double _inc3volt;



        public ICommand BackCommand { get; }

        public ICommand RelayPage { get; }

        private readonly SafePoller _liveDataTimer;

        public ObservableCollection<MeterViewModel> Meters { get; }
        private readonly INavigationService _navigation;

        private CancellationTokenSource _cts;
        public EnergyMonitorViewModel2(INavigationService navigation )
        {
 

            _navigation = navigation;
            _liveDataTimer = new SafePoller(TimeSpan.FromMilliseconds(1000), PollAsync, ex=>Console.WriteLine(ex.Message));
            _liveDataTimer.Start();
           
            //_meterService.Connect("COM6", 19200, 10);
            BackCommand = new RelayCommand(NavigateToHome);
            RelayPage = new RelayCommand(NavigateToRelayPage);

            INC1AMP = 0.0;
            INC2AMP = 0.0;
            INC3AMP = 0.0;
            INC1VOLT = 0.0;
            INC2VOLT = 0.0;
            INC3VOLT = 0.0;

        }

        private void NavigateToHome()
        {
            _navigation.NavigateTo<Dashboard1ViewModel>();
        }

        private void NavigateToRelayPage()
        {
            _navigation.NavigateTo<Iec61850MonitorViewModel>();
        }
        private async Task PollAsync(Dictionary<int, object> parameters)
        {
            try
            {
                //var reading = await _meterService.ReadAsync();

                //VoltageA_N = reading.VoltageA_N;
                //VoltageB_N = reading.VoltageB_N;
                //VoltageC_N = reading.VoltageC_N;
                //VoltageAvg = reading.VoltageL_N_Avg;

                //CurrentA = reading.CurrentA;
                //CurrentB = reading.CurrentB;
                //CurrentC = reading.CurrentC;
                //CurrentAvg = reading.CurrentAvg;

                //TotalActivePower = reading.TotalActivePower;
                //TotalReactivePower = reading.TotalReactivePower;
                //TotalApparentPower = reading.TotalApparentPower;

                //Frequency = reading.Frequency;
                //TotalPowerFactor = reading.TotalPowerFactor;

                //INC1VOLT = VoltageA_N;
                //INC2VOLT = VoltageB_N;
                //INC3VOLT = VoltageC_N;

                //INC1AMP = Frequency;


            }
            catch (Exception ex)
            {
                // log/show error state
                Console.WriteLine($"Poll error: {ex.Message}");
            }
        }

        private async Task LiveDataTimerTick(Dictionary<int, object> parameters)
        {
            // Simulate updating live data
            INC1AMP = 45 + new Random().NextDouble() * 6;
            INC2AMP = 35 + new Random().NextDouble() * 6;
            INC3AMP = 40 + new Random().NextDouble() * 4;
            INC1VOLT = 9 + new Random().NextDouble() * 2;
            INC2VOLT = 8 + new Random().NextDouble() * 2;
            INC3VOLT = 10 + new Random().NextDouble() * 2;
            await Task.CompletedTask; // Placeholder for any asynchronous operations

            if (INC1AMP > 50 )
            {
                Brush1 = Brushes.Red;
                
            }
            if(INC1AMP < 48 && INC1AMP > 45)
            {
                Brush1 = Brushes.Orange;
            }
            else
            {
                Brush1 = Brushes.LimeGreen;

            }



            if (INC2AMP > 40)
            {
                Brush2 = Brushes.Red;

            }
            if (INC2AMP < 38 && INC2AMP > 36)
            {
                Brush2 = Brushes.Orange;
            }
            else
            {
                Brush2 = Brushes.LimeGreen;

            }


        }

        public void Dispose() => _liveDataTimer?.Dispose();
    }
}
