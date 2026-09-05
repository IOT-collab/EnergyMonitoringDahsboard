using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC.Shared.Models
{


    public class RegisterConfig
    {
        public string ParameterName { get; set; }

        public ushort RegisterAddress { get; set; }

        // Protocol-neutral address. For Modbus this is optional and the legacy
        // RegisterAddress/DataArea fields remain authoritative. For OPC UA it
        // is the NodeId (for example: ns=2;s=Plant/Meter1/VoltageA); for OPC DA
        // it is the server ItemId/tag name.
        public string? Address { get; set; }

        public ModbusDataArea DataArea { get; set; } = ModbusDataArea.HoldingRegister;

        public bool IsEnabled { get; set; } = true;

        public string Unit { get; set; }

        public float ScaleFactor { get; set; } = 1;

        public int Length { get; set; } = 2;

        // Changed from string to enum for stronger typing and easier binding
        public RegisterDataType DataType { get; set; } = RegisterDataType.Float;


    }
}
