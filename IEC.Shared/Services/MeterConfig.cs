using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC.Shared.Services
{
    public class MeterConfig
    {
        public string MeterName { get; set; }
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public Parity Parity { get; set; }
        public byte SlaveId { get; set; }
    }   
}
