using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace IEC.Shared.Models
{
    public class MeterViewModel : ObservableObjectVM   // or whatever your base class is
    {
        private string _meterName;
        private string _meterstatus;
        private float _voltageA_N;
        private float _voltageB_N;
        private float _voltageC_N;
        private float _voltageL_N_Avg;
        private float _currentA;
        private float _currentB;
        private float _currentC;
        private float _currentAvg;
        private float _totalActivePower;
        private float _totalReactivePower;
        private float _totalApparentPower;
        private float _frequency;
        private float _totalPowerFactor;

        public string MeterName
        {
            get => _meterName;
            set => SetProperty(ref _meterName, value);
        }

        public string MeterStatus
        {
            get => _meterstatus;
            set => SetProperty(ref _meterstatus, value);
        }

        public float VoltageA_N
        {
            get => _voltageA_N;
            set => SetProperty(ref _voltageA_N, value);
        }

        public float VoltageB_N
        {
            get => _voltageB_N;
            set => SetProperty(ref _voltageB_N, value);
        }

        public float VoltageC_N
        {
            get => _voltageC_N;
            set => SetProperty(ref _voltageC_N, value);
        }

        public float VoltageL_N_Avg
        {
            get => _voltageL_N_Avg;
            set => SetProperty(ref _voltageL_N_Avg, value);
        }

        public float CurrentA
        {
            get => _currentA;
            set => SetProperty(ref _currentA, value);
        }

        public float CurrentB
        {
            get => _currentB;
            set => SetProperty(ref _currentB, value);
        }

        public float CurrentC
        {
            get => _currentC;
            set => SetProperty(ref _currentC, value);
        }

        public float CurrentAvg
        {
            get => _currentAvg;
            set => SetProperty(ref _currentAvg, value);
        }

        public float TotalActivePower
        {
            get => _totalActivePower;
            set => SetProperty(ref _totalActivePower, value);
        }

        public float TotalReactivePower
        {
            get => _totalReactivePower;
            set => SetProperty(ref _totalReactivePower, value);
        }

        public float TotalApparentPower
        {
            get => _totalApparentPower;
            set => SetProperty(ref _totalApparentPower, value);
        }

        public float Frequency
        {
            get => _frequency;
            set => SetProperty(ref _frequency, value);
        }

        public float TotalPowerFactor
        {
            get => _totalPowerFactor;
            set => SetProperty(ref _totalPowerFactor, value);
        }
    }
}
