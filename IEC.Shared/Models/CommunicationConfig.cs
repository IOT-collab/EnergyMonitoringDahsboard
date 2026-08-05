using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IEC.Shared.Models
{
    public enum RegisterWordOrder
    {
        LowHigh = 0,
        HighLow = 1
    }

    public class CommunicationConfig : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private ProtocolsType _protocol = ProtocolsType.ModbusRtu;
        public ProtocolsType Protocol
        {
            get => _protocol;
            set { if (_protocol == value) return; _protocol = value; Notify(); }
        }

        private string _comPort = "COM1";
        public string ComPort
        {
            get => _comPort;
            set { if (_comPort == value) return; _comPort = value; Notify(); }
        }

        private int _baudRate = 9600;
        public int BaudRate
        {
            get => _baudRate;
            set { if (_baudRate == value) return; _baudRate = value; Notify(); }
        }

        private string _parity = "None";
        public string Parity
        {
            get => _parity;
            set { if (_parity == value) return; _parity = value; Notify(); }
        }

        private int _dataBits = 8;
        public int DataBits
        {
            get => _dataBits;
            set { if (_dataBits == value) return; _dataBits = value; Notify(); }
        }

        private int _stopBits = 1;
        public int StopBits
        {
            get => _stopBits;
            set { if (_stopBits == value) return; _stopBits = value; Notify(); }
        }

        private byte _slaveId = 1;
        public byte SlaveId
        {
            get => _slaveId;
            set { if (_slaveId == value) return; _slaveId = value; Notify(); }
        }

        private RegisterWordOrder _wordOrder = RegisterWordOrder.LowHigh;
        public RegisterWordOrder WordOrder
        {
            get => _wordOrder;
            set { if (_wordOrder == value) return; _wordOrder = value; Notify(); }
        }

        public string? IpAddress { get; set; }
        public int TcpPort { get; set; } = 502;
    }
}
