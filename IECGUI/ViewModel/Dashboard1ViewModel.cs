using IEC.Shared.Services;
using IECGUI.Services;
using IECGUI.ViewModel;
using IPCSoftware.Common.CommonExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace IECGUI.ViewModel
{
    public class Dashboard1ViewModel : BaseViewModel
    {

        private readonly SafePoller _liveDataTimer;

        private readonly INavigationService _navigation;

        public ICommand EventsCommand { get; }

        public ICommand HomeCommand { get; }

        public ICommand AlarmCommand { get; }

        public ICommand TrendingCommand { get; }

        public ICommand MenuCommand { get; }

        public ICommand INCBRK1OPEN { get; set; }

        public ICommand INCBRK2OPEN { get; set; }

        public ICommand INCBRK3OPEN { get; set; }

        public ICommand INCBRK4OPEN { get; set; }

        public ICommand BCBRKOPEN { get; set; }

        public ICommand OUTBRK1OPEN { get; set; }
        public ICommand OUTBRK2OPEN { get; set; }

        public ICommand OUTBRK3OPEN { get; set; }
        public ICommand OUTBRK4OPEN { get; set; }
        public ICommand OUTBRK5OPEN { get; set; }
        public ICommand OUTBRK6OPEN { get; set; }

        public ICommand OUTBRK7OPEN { get; set; }
        public ICommand OUTBRK8OPEN { get; set; }

        public ICommand OUTBRK9OPEN { get; set; }


        //Breaker Status Close
        private Visibility _breakerstatusclose1;
        public Visibility breakerstatusclose1
        {
            get => _breakerstatusclose1;
            set => SetProperty(ref _breakerstatusclose1, value);
        }

        private Visibility _breakerstatusclose2;
        public Visibility breakerstatusclose2
        {
            get => _breakerstatusclose2;
            set => SetProperty(ref _breakerstatusclose2, value);
        }
        private Visibility _breakerstatusclose3;
        public Visibility breakerstatusclose3
        {
            get => _breakerstatusclose3;
            set => SetProperty(ref _breakerstatusclose3, value);
        }

        private Visibility _breakerstatusclose4;
        public Visibility breakerstatusclose4
        {
            get => _breakerstatusclose4;
            set => SetProperty(ref _breakerstatusclose4, value);
        }

        private Visibility _breakerstatusclose5;
        public Visibility breakerstatusclose5
        {
            get => _breakerstatusclose5;
            set => SetProperty(ref _breakerstatusclose5, value);
        }

        private Visibility _breakerstatusclose6;
        public Visibility breakerstatusclose6
        {
            get => _breakerstatusclose6;
            set => SetProperty(ref _breakerstatusclose6, value);
        }
        private Visibility _breakerstatusclose7;
        public Visibility breakerstatusclose7
        {
            get => _breakerstatusclose7;
            set => SetProperty(ref _breakerstatusclose7, value);
        }

        private Visibility _breakerstatusclose8;
        public Visibility breakerstatusclose8
        {
            get => _breakerstatusclose8;
            set => SetProperty(ref _breakerstatusclose8, value);
        }

        private Visibility _breakerstatusclose9;
        public Visibility breakerstatusclose9
        {
            get => _breakerstatusclose9;
            set => SetProperty(ref _breakerstatusclose9, value);
        }

        private Visibility _breakerstatusclose10;
        public Visibility breakerstatusclose10
        {
            get => _breakerstatusclose10;
            set => SetProperty(ref _breakerstatusclose10, value);
        }

        private Visibility _breakerstatusclose11;
        public Visibility breakerstatusclose11
        {
            get => _breakerstatusclose11;
            set => SetProperty(ref _breakerstatusclose11, value);
        }

        private Visibility _breakerstatusclose12;

        public Visibility breakerstatusclose12
        {
            get => _breakerstatusclose12;
            set => SetProperty(ref _breakerstatusclose12, value);
        }

        private Visibility _breakerstatusclose13;
        public Visibility breakerstatusclose13
        {   get => _breakerstatusclose13;
            set => SetProperty(ref _breakerstatusclose13, value);
        }

        private Visibility _breakerstatusclose14;
        public Visibility breakerstatusclose14
        {
            get => _breakerstatusclose14;
            set => SetProperty(ref _breakerstatusclose14, value);
        }





        // Braker Status Open

        private Visibility _breakerstatusopen1;

        public Visibility breakerstatusopen1
        {
            get => _breakerstatusopen1;
            set => SetProperty(ref _breakerstatusopen1, value)   ;
        }

        private Visibility _breakerstatusopen2;

        public Visibility breakerstatusopen2
        {
            get => _breakerstatusopen2;
            set => SetProperty(ref _breakerstatusopen2, value);
        }

        private Visibility _breakerstatusopen3;

        public Visibility breakerstatusopen3
        {
            get => _breakerstatusopen3;
            set => SetProperty(ref _breakerstatusopen3, value);
        }

        private Visibility _breakerstatusopen4;

        public Visibility breakerstatusopen4
        {
            get => _breakerstatusopen4;
            set => SetProperty(ref _breakerstatusopen4, value);
        }


        private Visibility _breakerstatusopen5;

        public Visibility breakerstatusopen5
        {
            get => _breakerstatusopen5;
            set => SetProperty(ref _breakerstatusopen5, value);
        }

        private Visibility _breakerstatusopen6;

        public Visibility breakerstatusopen6
        {
            get => _breakerstatusopen6;
            set => SetProperty(ref _breakerstatusopen6, value);
        }

        private Visibility _breakerstatusopen7;
        public Visibility breakerstatusopen7
        {
            get => _breakerstatusopen7;
            set => SetProperty(ref _breakerstatusopen7, value);
        }

        private Visibility _breakerstatusopen8;
        public Visibility breakerstatusopen8
        {
            get => _breakerstatusopen8;
            set => SetProperty(ref _breakerstatusopen8, value);
        }


        private Visibility _breakerstatusopen9;
        public Visibility breakerstatusopen9
        {
            get => _breakerstatusopen9;
            set => SetProperty(ref _breakerstatusopen9, value);
        }


        private Visibility _breakerstatusopen10;
        public Visibility breakerstatusopen10
        {
            get => _breakerstatusopen10;
            set => SetProperty(ref _breakerstatusopen10, value);
        }


        private Visibility _breakerstatusopen11;
        public Visibility breakerstatusopen11
        {
            get => _breakerstatusopen11;
            set => SetProperty(ref _breakerstatusopen11, value);
        }

        private Visibility _breakerstatusopen12;
        public Visibility breakerstatusopen12
        {
            get => _breakerstatusopen12; 
            set => SetProperty(ref _breakerstatusopen12, value);
        }



        private Visibility _breakerstatusopen13;
        public Visibility breakerstatusopen13
        {
            get => _breakerstatusopen13;
            set => SetProperty(ref _breakerstatusopen13, value);
        }


        private Visibility _breakerstatusopen14;
        public Visibility breakerstatusopen14
        {
            get => _breakerstatusopen14;
            set => SetProperty(ref _breakerstatusopen14, value);
        }

        //Feeder Line Color

        private Brush _feeder1LineColor;
        public Brush Feeder1LineColor
        {
            get => _feeder1LineColor;
            set => SetProperty(ref _feeder1LineColor, value);
        }

        private Brush _feeder2LineColor;
        public Brush Feeder2LineColor
        {
            get => _feeder2LineColor;
            set => SetProperty(ref _feeder2LineColor, value);
        }

        private Brush _feeder3LineColor;
        public Brush Feeder3LineColor
        {
            get => _feeder3LineColor;
            set => SetProperty(ref _feeder3LineColor, value);
        }

        private Brush _feeder4LineColor;
        public Brush Feeder4LineColor
        {
            get => _feeder4LineColor;
            set => SetProperty(ref _feeder4LineColor, value);
        }

        private Brush _feeder5LineColor;
        public Brush Feeder5LineColor
        {
            get => _feeder5LineColor;
            set => SetProperty(ref _feeder5LineColor, value);
        }

        private Brush _feeder6LineColor;
        public Brush Feeder6LineColor
        {
            get => _feeder6LineColor;
            set => SetProperty(ref _feeder6LineColor, value);
        }

        private Brush _feeder7LineColor;
        public Brush Feeder7LineColor
        {
            get => _feeder7LineColor;
            set => SetProperty(ref _feeder7LineColor, value);
        }

        private Brush _feeder8LineColor;
        public Brush Feeder8LineColor
        {
            get => _feeder8LineColor;
            set => SetProperty(ref _feeder8LineColor, value);
        }

        private Brush _feeder9LineColor;
        public Brush Feeder9LineColor
        {
            get => _feeder9LineColor;
            set => SetProperty(ref _feeder9LineColor, value);
        }

        private Brush _feeder10LineColor;
        public Brush Feeder10LineColor
        {
            get => _feeder10LineColor;
            set => SetProperty(ref _feeder10LineColor, value);
        }

        private Brush _feeder11LineColor;
        public Brush Feeder11LineColor
        {
            get => _feeder11LineColor;
            set => SetProperty(ref _feeder11LineColor, value);
        }

        private Brush _feeder12LineColor;
        public Brush Feeder12LineColor
        {
            get => _feeder12LineColor;
            set => SetProperty(ref _feeder12LineColor, value);
        }
        private Brush _feeder13LineColor;
        public Brush Feeder13LineColor
        {
            get => _feeder13LineColor;
            set => SetProperty(ref _feeder13LineColor, value);
        }

        private Brush _feeder14LineColor;
        public Brush Feeder14LineColor
        {
            get => _feeder14LineColor;
            set => SetProperty(ref _feeder14LineColor, value);
        }

        private Brush _feeder15LineColor;
        public Brush Feeder15LineColor
        {
            get => _feeder15LineColor;
            set => SetProperty(ref _feeder15LineColor, value);
        }

        private Brush _feeder16LineColor;
        public Brush Feeder16LineColor
        {
            get => _feeder16LineColor;
            set => SetProperty(ref _feeder16LineColor, value);
        }

        private Brush _feeder17LineColor;
        public Brush Feeder17LineColor
        {
            get => _feeder17LineColor;
            set => SetProperty(ref _feeder17LineColor, value);
        }






        // Feeder Arrow Staus

        private Visibility _feeder1ArrowStatus;

        public Visibility Feeder1ArrowStatus
        {
            get => _feeder1ArrowStatus;
            set => SetProperty(ref _feeder1ArrowStatus, value);
        }

        private Visibility _feeder2ArrowStatus;

        public Visibility Feeder2ArrowStatus
        {
            get => _feeder2ArrowStatus;
            set => SetProperty(ref _feeder2ArrowStatus, value);
        }

        public Visibility _feeder3ArrowStatus;
        public Visibility Feeder3ArrowStatus
        {
            get => _feeder3ArrowStatus;
            set => SetProperty(ref _feeder3ArrowStatus, value);
        }

        private Visibility _feeder4ArrowStatus;
        public Visibility Feeder4ArrowStatus
        {
            get => _feeder4ArrowStatus;
            set => SetProperty(ref _feeder4ArrowStatus, value);
        }

        private Visibility _feeder5ArrowStatus;
        public Visibility Feeder5ArrowStatus
        {
            get => _feeder5ArrowStatus;
            set => SetProperty(ref _feeder5ArrowStatus, value);
        }
        private Visibility _feeder6ArrowStatus;
        public Visibility Feeder6ArrowStatus
        {
            get => _feeder6ArrowStatus;
            set => SetProperty(ref _feeder6ArrowStatus, value);
        }

        private Visibility _feeder7ArrowStatus;
        public Visibility Feeder7ArrowStatus
        {
            get => _feeder7ArrowStatus;
            set => SetProperty(ref _feeder7ArrowStatus, value);
        }
        private Visibility _feeder8ArrowStatus;
        public Visibility Feeder8ArrowStatus
        {
            get => _feeder8ArrowStatus;
            set => SetProperty(ref _feeder8ArrowStatus, value);
        }

        private Visibility _feeder9ArrowStatus;
        public Visibility Feeder9ArrowStatus
        {
            get => _feeder9ArrowStatus;
            set => SetProperty(ref _feeder9ArrowStatus, value);
        }

        private Visibility _feeder10ArrowStatus;
        public Visibility Feeder10ArrowStatus
        {
            get => _feeder10ArrowStatus;
            set => SetProperty(ref _feeder10ArrowStatus, value);
        }

        private Visibility _feeder11ArrowStatus;
        public Visibility Feeder11ArrowStatus
        {
            get => _feeder11ArrowStatus;
            set => SetProperty(ref _feeder11ArrowStatus, value);
        }

        private Visibility _feeder12ArrowStatus;
        public Visibility Feeder12ArrowStatus
        {
            get => _feeder12ArrowStatus;
            set => SetProperty(ref _feeder12ArrowStatus, value);
        }

        private Visibility _feeder13ArrowStatus;
        public Visibility Feeder13ArrowStatus
        {
            get => _feeder13ArrowStatus;
            set => SetProperty(ref _feeder13ArrowStatus, value);
        }

        private Visibility _feeder14ArrowStatus;
        public Visibility Feeder14ArrowStatus
        {
            get => _feeder14ArrowStatus;
            set => SetProperty(ref _feeder14ArrowStatus, value);
        }

        private Visibility _feeder15ArrowStatus;

        public Visibility Feeder15ArrowStatus
        {
            get => _feeder15ArrowStatus;
            set => SetProperty(ref _feeder15ArrowStatus, value);
        }

        private Visibility _feeder16ArrowStatus;
        public Visibility Feeder16ArrowStatus
        {
            get => _feeder16ArrowStatus;
            set => SetProperty(ref _feeder16ArrowStatus, value);
        }

        private Visibility _feeder17ArrowStatus;
        public Visibility Feeder17ArrowStatus
        {
            get => _feeder17ArrowStatus;
            set => SetProperty(ref _feeder17ArrowStatus, value);
        }

        private Visibility _feeder18ArrowStatus;
        public Visibility Feeder18ArrowStatus
        {
            get => _feeder18ArrowStatus;
            set => SetProperty(ref _feeder18ArrowStatus, value);
        }


        private bool _status1 =false;
        private bool _status2 = false;
        private bool _status3 = false;
        private bool _status4 = false;
        private bool _status5 = false;
        private bool _status6 = false;
        private bool _status7 = false;
        private bool _status8 = false;
        private bool _status9 = false;
        private bool _status10 = false;
        private bool _status11 = false;
        private bool _status12 = false;
        private bool _status13 = false;
        private bool _status14 = false;



        public Dashboard1ViewModel(INavigationService navigation )
        {

            _navigation = navigation;
            EventsCommand = new RelayCommand(OpenRelayCard);
            HomeCommand = new RelayCommand(() => _navigation.NavigateTo<EnergyMonitorViewModel>()); //_navigation.NavigateTo(new Dashboard1ViewModel(_navigation));
            AlarmCommand = new RelayCommand(() => _navigation.NavigateTo<EnergyMonitorViewModel2>());
            TrendingCommand = new RelayCommand(() => _navigation.NavigateTo<ConfigurationViewModel>());
            MenuCommand = new RelayCommand(() => _navigation.NavigateTo<HomePageViewModel>());


            _liveDataTimer = new SafePoller(TimeSpan.FromMilliseconds(100), RunBackgroundService, ex => Console.WriteLine(ex.Message));
            _liveDataTimer.Start();

            OUTBRK1OPEN = new RelayCommand(OUTBRK1OPEN_Execute);
            OUTBRK2OPEN = new RelayCommand(OUTBRK2OPEN_Execute);
            OUTBRK3OPEN = new RelayCommand(OUTBRK3OPEN_Execute);
            OUTBRK4OPEN = new RelayCommand(OUTBRK4OPEN_Execute);
            OUTBRK5OPEN = new RelayCommand(OUTBRK5OPEN_Execute);
            OUTBRK6OPEN = new RelayCommand(OUTBRK6OPEN_Execute);
            OUTBRK7OPEN = new RelayCommand(OUTBRK7OPEN_Execute);
            OUTBRK8OPEN = new RelayCommand(OUTBRK8OPEN_Execute);
            OUTBRK9OPEN = new RelayCommand(OUTBRK9OPEN_Execute);

            INCBRK1OPEN = new RelayCommand(INCBRK10OPEN_Execute);
            INCBRK2OPEN = new RelayCommand(INCBRK20OPEN_Execute);
            INCBRK3OPEN = new RelayCommand(INCBRK30OPEN_Execute);
            INCBRK4OPEN = new RelayCommand(INCBRK40OPEN_Execute);
            BCBRKOPEN = new RelayCommand(BCBRK30OPEN_Execute);

            breakerstatusclose1 = Visibility.Hidden;
            breakerstatusclose2 = Visibility.Hidden;
            breakerstatusclose3 = Visibility.Hidden;
            breakerstatusclose4 = Visibility.Hidden;
            breakerstatusclose5 = Visibility.Hidden;
            breakerstatusclose6 = Visibility.Hidden;
            breakerstatusclose7 = Visibility.Hidden;
            breakerstatusclose8 = Visibility.Hidden;
            breakerstatusclose9 = Visibility.Hidden;
            breakerstatusclose10 = Visibility.Hidden;
            breakerstatusclose11 = Visibility.Hidden;
            breakerstatusclose12 = Visibility.Hidden;
            breakerstatusclose13 = Visibility.Hidden;
            breakerstatusclose14 = Visibility.Hidden;
            //Feeder Line Color-
            Feeder1LineColor = Brushes.Gray;
            Feeder2LineColor = Brushes.Gray;
            Feeder3LineColor = Brushes.Gray;
            Feeder4LineColor = Brushes.Gray;
            Feeder5LineColor = Brushes.Gray;
            Feeder6LineColor = Brushes.Gray;
            Feeder7LineColor = Brushes.Gray;
            Feeder8LineColor = Brushes.Gray;
            Feeder9LineColor = Brushes.Gray;
            Feeder10LineColor = Brushes.Gray;
            Feeder11LineColor = Brushes.Gray;
            Feeder12LineColor = Brushes.Gray;
            Feeder13LineColor = Brushes.Gray;
            Feeder14LineColor = Brushes.Gray;
            Feeder15LineColor = Brushes.Gray;
            Feeder16LineColor = Brushes.Gray;
            Feeder17LineColor = Brushes.Gray;



        }

        public async Task RunBackgroundService(Dictionary<int, object> parameters)
        {
            if(_status10 || _status11 || ((_status12  || _status14) && _status13)) { Feeder5LineColor = Brushes.Red; } else { Feeder5LineColor = Brushes.Gray; }
            if(_status12 || _status14 || (( _status10 || _status11) && _status13)) { Feeder6LineColor = Brushes.Red; } else { Feeder6LineColor = Brushes.Gray; }
            if(Feeder5LineColor != Brushes.Red) 
            {
                Feeder7LineColor = Brushes.Gray;
                Feeder8LineColor = Brushes.Gray;
                Feeder9LineColor = Brushes.Gray;
                Feeder10LineColor = Brushes.Gray;

            }
            if (Feeder6LineColor != Brushes.Red)
            {
                Feeder12LineColor = Brushes.Gray;
                Feeder13LineColor = Brushes.Gray;
                Feeder14LineColor = Brushes.Gray;
                Feeder15LineColor = Brushes.Gray;

            }

        }

        public void OpenRelayCard()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var win = new IECGUI.View.RelayCard();
                win.DataContext = new IECGUI.ViewModel.RelayCardViewModel(win, _navigation);
                win.ShowDialog();
            });

        }
        public void ExecuteEvents()
        {
           

        }

        public void OUTBRK1OPEN_Execute()
        {
            _status1 = !_status1;
            breakerstatusopen1 = !_status1 ? Visibility.Visible : Visibility.Hidden;
            breakerstatusclose1 = _status1 ? Visibility.Visible : Visibility.Hidden;
            Feeder7LineColor = _status1 ? Brushes.Red : Brushes.Gray;
        }
        public void OUTBRK2OPEN_Execute()
        {
            _status2 = !_status2;
            breakerstatusopen2 = !_status2 ? Visibility.Visible : Visibility.Hidden;
            breakerstatusclose2 = _status2 ? Visibility.Visible : Visibility.Hidden;
            Feeder8LineColor = _status2 ? Brushes.Red : Brushes.Gray;
        }

        public void OUTBRK3OPEN_Execute()
        {
            _status3 = !_status3;
            breakerstatusopen3 = !_status3 ? Visibility.Visible : Visibility.Hidden;
            breakerstatusclose3 = _status3 ? Visibility.Visible : Visibility.Hidden;
            Feeder9LineColor = _status3 ? Brushes.Red : Brushes.Gray;
        }
        public void OUTBRK4OPEN_Execute()
        {
            _status4 = !_status4;
            breakerstatusopen4 = !_status4 ? Visibility.Visible : Visibility.Hidden;
            breakerstatusclose4 = _status4 ? Visibility.Visible : Visibility.Hidden;
            Feeder10LineColor = _status4 ? Brushes.Red : Brushes.Gray;
        }

        public void OUTBRK5OPEN_Execute()
        {
            _status5 = !_status5;
            breakerstatusopen5 = !_status5 ? Visibility.Visible : Visibility.Hidden;
            breakerstatusclose5 = _status5 ? Visibility.Visible : Visibility.Hidden;
            Feeder11LineColor = _status5 ? Brushes.Red : Brushes.Gray;
        }
        public void OUTBRK6OPEN_Execute()
        {
            _status6 = !_status6;
            breakerstatusopen6 = !_status6 ? Visibility.Visible : Visibility.Hidden;
            breakerstatusclose6 = _status6 ? Visibility.Visible : Visibility.Hidden;
            Feeder12LineColor = _status6 ? Brushes.Red : Brushes.Gray;
        }
        public void OUTBRK7OPEN_Execute()
        {
            _status7 = !_status7;
            breakerstatusopen7 = !_status7 ? Visibility.Visible : Visibility.Hidden;
            breakerstatusclose7 = _status7 ? Visibility.Visible : Visibility.Hidden;
            Feeder13LineColor = _status7 ? Brushes.Red : Brushes.Gray;
        }
        public void OUTBRK8OPEN_Execute()
        {
            _status8 = !_status8;
            breakerstatusopen8 = !_status8 ? Visibility.Visible : Visibility.Hidden;
            breakerstatusclose8 = _status8 ? Visibility.Visible : Visibility.Hidden;
            Feeder14LineColor = _status8 ? Brushes.Red : Brushes.Gray;
        }
        public void OUTBRK9OPEN_Execute()
        {
            _status9 = !_status9;
            breakerstatusopen9 = !_status9 ? Visibility.Visible : Visibility.Hidden;
            breakerstatusclose9 = _status9 ? Visibility.Visible : Visibility.Hidden;
            Feeder15LineColor = _status9 ? Brushes.Red : Brushes.Gray;
        }

        public void INCBRK10OPEN_Execute()
        {
            _status10 = !_status10;
            breakerstatusopen10 = !_status10 ? Visibility.Visible : Visibility.Hidden;
            breakerstatusclose10 = _status10 ? Visibility.Visible : Visibility.Hidden;
            Feeder1LineColor = _status10 ? Brushes.Red : Brushes.Gray;
            
        }

        public void INCBRK20OPEN_Execute()
        {
            _status11 = !_status11;
            breakerstatusopen11 = !_status11 ? Visibility.Visible : Visibility.Hidden;
            breakerstatusclose11 = _status11 ? Visibility.Visible : Visibility.Hidden;
            Feeder2LineColor = _status11 ? Brushes.Red : Brushes.Gray;
        }

        public void INCBRK30OPEN_Execute()
        {
            _status12 = !_status12;
            breakerstatusopen12 = !_status12 ? Visibility.Visible : Visibility.Hidden;
            breakerstatusclose12 = _status12 ? Visibility.Visible : Visibility.Hidden;
            Feeder3LineColor = _status12 ? Brushes.Red : Brushes.Gray;
        }


        public void BCBRK30OPEN_Execute()
        {
            _status13 = !_status13;
            breakerstatusopen13 = !_status13 ? Visibility.Visible : Visibility.Hidden;
            breakerstatusclose13 = _status13 ? Visibility.Visible : Visibility.Hidden;
            
           // Feeder6LineColor = _status13 ? Brushes.Red : Brushes.Gray;
        }


        public void INCBRK40OPEN_Execute()
        {
            _status14 = !_status14;
            breakerstatusopen14 = !_status14 ? Visibility.Visible : Visibility.Hidden;
            breakerstatusclose14 = _status14 ? Visibility.Visible : Visibility.Hidden;
            Feeder4LineColor = _status14  ? Brushes.Red : Brushes.Gray;
        }


    }
}
