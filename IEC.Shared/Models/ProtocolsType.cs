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
        OpcDa,

        // Mitsubishi MC protocol over TCP (SLMP / 3E frame).
        McSlmp,

        // Siemens S7 communication over the PLC's PROFINET Ethernet interface
        // (ISO-on-TCP/S7, normally TCP port 102). This is distinct from a
        // full PROFINET-IO controller/device stack.
        ProfinetS7
    }
}
