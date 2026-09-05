# OPC protocol setup

The meter configuration now supports four protocols:

| Protocol | Connection fields | Mapping address |
| --- | --- | --- |
| Modbus RTU/TCP | Existing serial/IP fields | `RegisterAddress` + `DataArea` |
| OPC UA | `OpcEndpointUrl`, security policy, optional credentials/certificate | `RegisterConfig.Address` = NodeId, e.g. `ns=2;s=Plant/Meter1/VoltageA` |
| OPC DA | `OpcServerName` (registered ProgID) and `OpcHost` | `RegisterConfig.Address` = ItemId/tag, e.g. `Channel1.Meter1.VoltageA` |

OPC UA and OPC DA are different technologies and require different client
libraries. This application now includes the OPC Foundation UA .NET Standard
client and `OpcUaDeviceService` opens a UA session, reads each enabled NodeId,
and converts the result into the same meter-reading contract used by Modbus.
OPC DA is Windows COM/DCOM and still requires the OPC server vendor's
Automation/interop SDK, matching process bitness and DCOM permissions. A DA
server must also be installed and registered on the local or remote host;
the current DA adapter reports a clear not-configured status until that SDK is
selected.

The Modbus transports remain unchanged. The configuration page, JSON
persistence, coordinator, Energy Monitor and Gauge views all consume the
shared meter reading contract, so a successful UA read automatically appears
in those views.

Do not enter a Modbus register number as an OPC address. Browse the OPC server
namespace/item tree and copy the exact NodeId (UA) or ItemId (DA) into the
`OPC NodeId / ItemId` column.
