using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC.Shared.IECModels
{
   
    public class RelayReadingModel : ObservableObjectVM  // must implement INotifyPropertyChanged
    {
        private bool _isOnline;
        public bool IsOnline
        {
            get => _isOnline;
            set { _isOnline = value; OnPropertyChanged(); }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        private int _relayId;
        public int RelayId
        {
            get => _relayId;
            set { _relayId = value; OnPropertyChanged(); }
        }

        private string _relayName;
        public string RelayName
        {
            get => _relayName;
            set { _relayName = value; OnPropertyChanged(); }
        }

        private Dictionary<string, float> _values = new();
        public Dictionary<string, float> Values
        {
            get => _values;
            set { _values = value; OnPropertyChanged(); }
        }

        // Convenience properties
        public float Hz => Values.GetValueOrDefault("Hz");
        public float PhV_A => Values.GetValueOrDefault("PhV_A");
        public float PhV_B => Values.GetValueOrDefault("PhV_B");
        public float PhV_C => Values.GetValueOrDefault("PhV_C");
        public float PPV_AB => Values.GetValueOrDefault("PPV_AB");
        public float PPV_BC => Values.GetValueOrDefault("PPV_BC");
        public float PPV_CA => Values.GetValueOrDefault("PPV_CA");
        public float A_PhsA => Values.GetValueOrDefault("A_PhsA");
        public float A_PhsB => Values.GetValueOrDefault("A_PhsB");
        public float A_PhsC => Values.GetValueOrDefault("A_PhsC");
        public float TotW => Values.GetValueOrDefault("TotW");
        public float TotVA => Values.GetValueOrDefault("TotVA");
        public float TotVAr => Values.GetValueOrDefault("TotVAr");
        public float TotPF => Values.GetValueOrDefault("TotPF");

        // Update properties in-place — no new object, no selection reset
        public void UpdateFrom(RelayReadingModel source)
        {
            IsOnline = source.IsOnline;
            ErrorMessage = source.ErrorMessage;
            Values = source.Values;

            // Notify all convenience properties at once
            OnPropertyChanged(nameof(Hz));
            OnPropertyChanged(nameof(PhV_A));
            OnPropertyChanged(nameof(PhV_B));
            OnPropertyChanged(nameof(PhV_C));
            OnPropertyChanged(nameof(PPV_AB));
            OnPropertyChanged(nameof(PPV_BC));
            OnPropertyChanged(nameof(PPV_CA));
            OnPropertyChanged(nameof(A_PhsA));
            OnPropertyChanged(nameof(A_PhsB));
            OnPropertyChanged(nameof(A_PhsC));
            OnPropertyChanged(nameof(TotW));
            OnPropertyChanged(nameof(TotVA));
            OnPropertyChanged(nameof(TotVAr));
            OnPropertyChanged(nameof(TotPF));
        }
    }

}
