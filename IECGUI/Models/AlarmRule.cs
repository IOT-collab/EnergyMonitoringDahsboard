using System;

namespace IECGUI.Models
{
    public class AlarmRule
    {
        public string AlarmName { get; set; } = string.Empty;
        public string MeterName { get; set; } = string.Empty;
        public string ParameterName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public double? LowWarning { get; set; }
        public double? LowAlarm { get; set; }
        public double? HighWarning { get; set; }
        public double? HighAlarm { get; set; }
        public int DelaySeconds { get; set; } = 3;
        public double ResetMargin { get; set; } = 0;
        public bool IsEnabled { get; set; } = true;

        public string RangeText
        {
            get
            {
                var low = LowAlarm?.ToString("F1") ?? LowWarning?.ToString("F1") ?? "-";
                var high = HighAlarm?.ToString("F1") ?? HighWarning?.ToString("F1") ?? "-";
                return $"{low} - {high} {Unit}";
            }
        }
    }
}
