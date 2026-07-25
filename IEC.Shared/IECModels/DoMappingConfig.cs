using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace IEC.Shared.IECModels
{
    public enum ValueType { Direct, Phase }

    public class DoMappingConfig
    {
        public string ParameterName { get; set; }  // "Frequency"
        public int DOIndex { get; set; }            // 4
        public ValueType ValueType { get; set; }    // Direct / Phase
        public int PhaseIndex { get; set; } = -1;  // -1 = N/A, 0=neut, 1=phsA
        public string Unit { get; set; }            // "Hz"
        public bool IsEnabled { get; set; } = true;
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public float AlarmLow { get; set; }
        public float AlarmHigh { get; set; }
    }
}