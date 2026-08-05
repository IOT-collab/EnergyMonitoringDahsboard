using System;

namespace IECGUI.Models
{
    public class AlarmLogEntry : ObservableObjectVM
    {
        private AlarmState _state;
        private bool _isAcknowledged;

        public string AlarmName { get; set; } = string.Empty;
        public string MeterName { get; set; } = string.Empty;
        public string ParameterName { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public AlarmSeverity Severity { get; set; }
        public DateTime RaisedAt { get; set; } = DateTime.Now;
        public DateTime? AcknowledgedAt { get; set; }
        public DateTime? ClearedAt { get; set; }
        public string Message { get; set; } = string.Empty;

        public AlarmState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                    OnPropertyChanged(nameof(StateText));
            }
        }

        public bool IsAcknowledged
        {
            get => _isAcknowledged;
            set => SetProperty(ref _isAcknowledged, value);
        }

        public string RaisedAtText => RaisedAt.ToString("dd-MMM-yyyy HH:mm:ss");
        public string ValueText => $"{Value:F2} {Unit}";
        public string SeverityText => Severity.ToString();
        public string StateText => State.ToString();
    }
}
