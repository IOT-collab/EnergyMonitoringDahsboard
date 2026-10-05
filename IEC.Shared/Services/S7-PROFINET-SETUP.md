# Siemens S7 / PROFINET setup

The first integration targets the normal SCADA use case: the application reads
and writes Siemens S7 variables through the PLC Ethernet interface. This is S7
communication over ISO-on-TCP (TCP port 102) carried by the PLC's PROFINET
interface. It is not a PROFINET-IO controller/device implementation.

## Meter connection

Select `ProfinetS7` for the meter and configure:

- PLC IP address
- CPU family (`S71200`, `S71500`, `S7300`, `S7400`, or `S7200`)
- Rack and slot from the Siemens hardware configuration (commonly rack `0`,
  slot `1` for S7-1200/1500; verify the actual project)

For S7-1200/1500, enable the PLC's `PUT/GET communication from remote partner`
setting when the PLC project requires it. Use the PLC's security policy and
access controls for production deployments.

## Address mappings

In the Address Mapping tab, enter an S7.Net variable address in the `S7
address` column. Examples:

- `DB1.DBX0.0` — Boolean bit
- `DB1.DBW2` — 16-bit word/int
- `DB1.DBD4` — 32-bit value/REAL
- `M10.0` — memory bit
- `I0.0` / `Q0.0` — input/output bit

Set the matching data type and scale factor. SCADA write buttons can write
Boolean and numeric S7 mappings using the same address.

## Scope boundary

S7.NetPlus is suitable for PLC variable access over Ethernet and supports the
common S7 CPU families. Full PROFINET-IO controller/device behavior (GSDML
device discovery, cyclic RT/IRT IO, alarms, and device ownership) needs a
separate PROFINET-IO stack and hardware/network validation; it should not be
silently represented as an S7 tag driver.
