using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IEC.Shared.Models
{
    public enum RegisterWordOrder
    {
        LowHigh = 0,  // low-word then high-word (service default)
        HighLow = 1   // high-word then low-word (common variant)
    }

    public class CommunicationConfig : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public ProtocolsType Protocol { get; set; } = ProtocolsType.ModbusRtu;

        // RTU fields
        public string ComPort { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
        public string Parity { get; set; } = "None";
        public int DataBits { get; set; } = 8;
        public int StopBits { get; set; } = 1;
        public byte SlaveId { get; set; } = 1;

        // New: per-meter default word order (can be changed from UI)
        public RegisterWordOrder WordOrder { get; set; } = RegisterWordOrder.LowHigh;

        // TCP fields (used when Protocol == ModbusTcp)
        // IP or hostname and optional TCP port (Modbus TCP default 502)
        public string IpAddress { get; set; }
        public int TcpPort { get; set; } = 502;
    }
}
