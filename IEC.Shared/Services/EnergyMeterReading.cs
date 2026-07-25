using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NModbus;
using NModbus.Extensions.Enron;
using NModbus.Utility;

namespace IEC.Shared.Services
{
    public class EnergyMeterReading
    {
        public float VoltageA_N { get; set; }
        public float VoltageB_N { get; set; }
        public float VoltageC_N { get; set; }
        public float VoltageL_N_Avg { get; set; }

        public float CurrentA { get; set; }
        public float CurrentB { get; set; }
        public float CurrentC { get; set; }
        public float CurrentAvg { get; set; }

        public float TotalActivePower { get; set; }
        public float TotalReactivePower { get; set; }
        public float TotalApparentPower { get; set; }

        public float Frequency { get; set; }
        public float TotalPowerFactor { get; set; }
    }
}
