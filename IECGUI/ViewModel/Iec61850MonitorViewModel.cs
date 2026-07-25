// IECGUI/ViewModel/Iec61850MonitorViewModel.cs
using IEC.Shared.IECInterface;
using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.Services;
using System.Windows.Input;
using System.Windows.Threading;

namespace IECGUI.ViewModel
{
    public class Iec61850MonitorViewModel : BaseViewModel
    {
        private readonly IIec61850MeterService _service;
        private readonly INavigationService _navigation;
        private readonly DispatcherTimer _pollTimer;

        // ── Connection Status ──────────────────────────
        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            set { _isOnline = value; OnPropertyChanged(); }
        }

        private string _statusMessage = "Disconnected";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // ── Frequency ─────────────────────────────────
        private float _hz;
        public float Hz
        {
            get => _hz;
            set { _hz = value; OnPropertyChanged(); }
        }

        // ── Phase Voltages (PhV) ───────────────────────
        private float _phV_A, _phV_B, _phV_C, _phV_Neut;
        public float PhV_A { get => _phV_A; set { _phV_A = value; OnPropertyChanged(); } }
        public float PhV_B { get => _phV_B; set { _phV_B = value; OnPropertyChanged(); } }
        public float PhV_C { get => _phV_C; set { _phV_C = value; OnPropertyChanged(); } }
        public float PhV_Neut { get => _phV_Neut; set { _phV_Neut = value; OnPropertyChanged(); } }

        // ── Line Voltages (PPV) ────────────────────────
        private float _ppV_AB, _ppV_BC, _ppV_CA;
        public float PPV_AB { get => _ppV_AB; set { _ppV_AB = value; OnPropertyChanged(); } }
        public float PPV_BC { get => _ppV_BC; set { _ppV_BC = value; OnPropertyChanged(); } }
        public float PPV_CA { get => _ppV_CA; set { _ppV_CA = value; OnPropertyChanged(); } }

        // ── Currents (A) ───────────────────────────────
        private float _a_PhsA, _a_PhsB, _a_PhsC, _a_Neut;
        public float A_PhsA { get => _a_PhsA; set { _a_PhsA = value; OnPropertyChanged(); } }
        public float A_PhsB { get => _a_PhsB; set { _a_PhsB = value; OnPropertyChanged(); } }
        public float A_PhsC { get => _a_PhsC; set { _a_PhsC = value; OnPropertyChanged(); } }
        public float A_Neut { get => _a_Neut; set { _a_Neut = value; OnPropertyChanged(); } }

        // ── Power ──────────────────────────────────────
        private float _totW, _totVA, _totVAr, _totPF;
        public float TotW { get => _totW; set { _totW = value; OnPropertyChanged(); } }
        public float TotVA { get => _totVA; set { _totVA = value; OnPropertyChanged(); } }
        public float TotVAr { get => _totVAr; set { _totVAr = value; OnPropertyChanged(); } }
        public float TotPF { get => _totPF; set { _totPF = value; OnPropertyChanged(); } }

        // ── Commands ───────────────────────────────────
        public ICommand BackCommand { get; }

        public Iec61850MonitorViewModel(
            INavigationService navigation,
            IIec61850MeterService service)
        {
            _navigation = navigation;
            _service = service;

            BackCommand = new RelayCommand(() =>
                _navigation.NavigateTo<EnergyMonitorViewModel2>());

            // Connect to relay
            try
            {
                _service.Connect("172.168.1.2", 102);
                StatusMessage = "Connected to Relay";
                IsOnline = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Connection Failed: {ex.Message}";
                IsOnline = false;
            }

            // Start polling every 1 second
            _pollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _pollTimer.Tick += async (s, e) => await PollAsync();
            _pollTimer.Start();
        }

        private async Task PollAsync()
        {
            var reading = await _service.ReadAsync();

            if (!reading.IsOnline)
            {
                IsOnline = false;
                StatusMessage = $"Error: {reading.ErrorMessage}";
                return;
            }

            IsOnline = true;
            StatusMessage = "Live";

            Hz = reading.Hz;

            PhV_A = reading.PhV_A;
            PhV_B = reading.PhV_B;
            PhV_C = reading.PhV_C;
            PhV_Neut = reading.PhV_Neut;

            PPV_AB = reading.PPV_AB;
            PPV_BC = reading.PPV_BC;
            PPV_CA = reading.PPV_CA;

            A_PhsA = reading.A_PhsA;
            A_PhsB = reading.A_PhsB;
            A_PhsC = reading.A_PhsC;
            A_Neut = reading.A_Neut;

            TotW = reading.TotW;
            TotVA = reading.TotVA;
            TotVAr = reading.TotVAr;
            TotPF = reading.TotPF;
        }

        public void Cleanup()
        {
            _pollTimer?.Stop();
            _service?.Disconnect();
        }
    }
}