namespace IEC.Shared.Models
{
    public class SldBreakerConfig
    {
        public string BreakerKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string MeterName { get; set; } = string.Empty;
        public ushort CommandAddress { get; set; }
        public ModbusDataArea CommandArea { get; set; } = ModbusDataArea.Coil;
        public ushort FeedbackAddress { get; set; }
        public ModbusDataArea FeedbackArea { get; set; } = ModbusDataArea.DiscreteInput;
        public bool FeedbackInverted { get; set; }
        public bool IsEnabled { get; set; }
    }
}
