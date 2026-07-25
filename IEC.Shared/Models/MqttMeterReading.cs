using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace IEC.Shared.Models
{
    public class MqttRawReading
    {
        public string StationId { get; set; }
        public string MeterId { get; set; }
        public string Topic { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string> Values { get; set; } = new();
    }
    public class MqttMeterReading
    {
        public string StationId { get; set; }
        public string MeterId { get; set; }
        public DateTime Timestamp { get; set; }

        // Voltage
        public float VoltageA { get; set; }
        public float VoltageB { get; set; }
        public float VoltageC { get; set; }
        public float VoltageAvg { get; set; }

        // Current
        public float CurrentA { get; set; }
        public float CurrentB { get; set; }
        public float CurrentC { get; set; }

        // Power
        public float TotalActivePower { get; set; }
        public float TotalReactivePower { get; set; }
        public float TotalApparentPower { get; set; }
        public float PowerFactor { get; set; }
        public float Frequency { get; set; }
    }
}