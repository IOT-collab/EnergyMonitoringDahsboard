using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IEC.Shared.Models
{
    public enum ProtocolsType
    {
        ModbusRtu,
        ModbusTcp,

        // OPC UA uses a secure, platform-independent endpoint and NodeId.
        OpcUa,

        // OPC DA is the legacy Windows COM/DCOM protocol and uses a server
        // ProgID plus an ItemId for each mapped value.
        OpcDa
        
    }
}
