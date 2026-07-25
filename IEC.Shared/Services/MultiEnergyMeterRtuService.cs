using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using NModbus;
using NModbus.Serial;
using NModbus.Utility;
using System.Diagnostics;
using IEC.Shared.Models;
using System.Windows;

namespace IEC.Shared.Services
{
    public class MultiEnergyMeterRtuService : IMultiEnergyMeterService
    {
        // One physical port shares one SerialPort + one master, serving multiple slave IDs
        private class PortConnection
        {
            public bool IsConnected { get; set; }
            public string Error { get; set; }
            public SerialPort Port;
            public IModbusSerialMaster Master;
        }

        private readonly Dictionary<string, PortConnection> _portConnections = new();
        // keyed by MeterName -> (PortName, SlaveId)
        private readonly Dictionary<string, (string PortName, byte SlaveId)> _meters = new();
        // store full meters configuration (registers + comm) so the service can read based on saved RegisterConfig
        private readonly Dictionary<string, MetersConfig> _meterConfigs = new();
        private readonly object _lock = new();

        // New: check whether this RTU service has configuration for the named meter
        public bool HasMeter(string meterName)
        {
            if (string.IsNullOrWhiteSpace(meterName))
                return false;

            // check configured map (contains both comm and registers) or runtime map used for port/slave
            return _meterConfigs.ContainsKey(meterName) || _meters.ContainsKey(meterName);
        }

        // Accept MetersConfig and map communication settings to port + slave id and keep meters config
        public async Task Configure(IEnumerable<MetersConfig> meters)
        {
            if (meters == null)
                return;

            foreach (var meter in meters)
            {
                if (string.IsNullOrWhiteSpace(meter?.MeterName))
                    continue;

                // store full config for later use (registers + comm)
                _meterConfigs[meter.MeterName] = meter;

                var comm = meter.Communication ?? new CommunicationConfig();

                string portName = comm.ComPort ?? "COM1";
                int baud = comm.BaudRate;
                byte slaveId = comm.SlaveId;

                _meters[meter.MeterName] = (portName, slaveId);

                // Open the physical port once per unique PortName (shared across meters on that bus)
                if (!_portConnections.ContainsKey(portName))
                {
                    var parity = System.IO.Ports.Parity.None;
                    if (!string.IsNullOrWhiteSpace(comm.Parity) &&
                        Enum.TryParse<System.IO.Ports.Parity>(comm.Parity, true, out var parsedParity))
                    {
                        parity = parsedParity;
                    }

                    var stopBits = StopBits.One;
                    // comm.StopBits is an int in the model: 0=None, 1=One, 2=Two
                    switch (comm.StopBits)
                    {
                        case 0: stopBits = StopBits.None; break;
                        case 2: stopBits = StopBits.Two; break;
                        default: stopBits = StopBits.One; break;
                    }

                    var port = new SerialPort(portName)
                    {
                        BaudRate = baud,
                        DataBits = comm.DataBits,
                        Parity = parity,
                        StopBits = stopBits
                    };

                    try
                    {
                       port.Open();
                    }
                    catch (Exception ex)
                    {
                        _portConnections[portName] = new PortConnection
                        {
                            IsConnected = false,
                            Error = ex.Message,
                            Port = port
                        };
                        // Continue: still store connection entry so failures are visible.
                        continue;
                    }

                    var factory = new ModbusFactory();
                    var transport = factory.CreateRtuTransport(port);
                    var master = factory.CreateMaster(transport);

                    master.Transport.ReadTimeout = 1000;   // ms
                    master.Transport.Retries = 2;

                    _portConnections[portName] = new PortConnection
                    {
                        Port = port,
                        Master = master,
                        IsConnected = port.IsOpen
                    };
                }
                else
                {
                    // If port exists but not open, try reopen
                    var conn = _portConnections[portName];
                    if (conn.Port != null && !conn.Port.IsOpen)
                    {
                        try
                        {
                            conn.Port.Open();
                            conn.IsConnected = conn.Port.IsOpen;
                        }
                        catch (Exception ex)
                        {
                            conn.IsConnected = false;
                            MessageBox.Show($"Unable to Connect: {ex.Message}");
                        }
                    }
                }
            }
        }

        // ReadAllAsync will iterate configured meter configs and read each using stored registers metadata
        public async Task<Dictionary<string, MeterReading>> ReadAllAsync()
        {
            var results = new Dictionary<string, MeterReading>();

            // snapshot keys to avoid collection-modified issues
            var meterNames = _meterConfigs.Keys.ToArray();

            foreach (var meterName in meterNames)
            {
                try
                {
                    var reading = await ReadOneAsync(meterName);
                    results[meterName] = reading;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Read failed for {meterName}: {ex.Message}");
                    results[meterName] = new MeterReading { MeterName = meterName };
                }

                await Task.Delay(50); // settle time before switching to next slave on the bus
            }

            return results;
        }

        // Forwarding overload that looks up stored Registers from configuration
        public Task<MeterReading> ReadOneAsync(string meterName)
        {
            if (!_meterConfigs.TryGetValue(meterName, out var cfg) || cfg == null)
                throw new InvalidOperationException($"No saved configuration (registers) for meter '{meterName}'.");

            return ReadOneAsync(meterName, cfg.Registers);
        }

        // Primary implementation that reads using provided registers metadata
        public Task<MeterReading> ReadOneAsync(string meterName, IEnumerable<RegisterConfig> registers)
        {
            if (!_meters.TryGetValue(meterName, out var meter))
                throw new InvalidOperationException($"Meter '{meterName}' is not configured.");

            if (!_portConnections.TryGetValue(meter.PortName, out var conn) || conn?.Port == null || !conn.Port.IsOpen || conn.Master == null)
                throw new InvalidOperationException($"Port for meter '{meterName}' is not connected.");

            return Task.Run(() =>
            {
                var reading = new MeterReading { MeterName = meterName, Timestamp = DateTime.UtcNow };

                lock (_lock)
                {
                    foreach (var reg in registers.Where(r => r.IsEnabled))
                    {
                        try
                        {
                            // Length is number of registers to read
                            int count = Math.Max(1, reg.Length);
                            ushort[] raw = conn.Master.ReadHoldingRegisters(meter.SlaveId, reg.RegisterAddress, (ushort)count);

                            // Use enum-typed DataType
                            object value = DecodeRegister(raw, reg.DataType);

                            // apply scale factor
                            if (value is double d)
                                value = d * reg.ScaleFactor;
                            else if (value is float f)
                                value = f * reg.ScaleFactor;
                            else if (value is int i)
                                value = (double)i * reg.ScaleFactor;

                            var key = string.IsNullOrWhiteSpace(reg.ParameterName) ? reg.RegisterAddress.ToString() : reg.ParameterName;
                            reading.Values[key] = value;
                        }
                        catch (Exception ex)
                        {
                            var key = string.IsNullOrWhiteSpace(reg.ParameterName) ? reg.RegisterAddress.ToString() : reg.ParameterName;
                            reading.Values[key] = null;
                            Console.WriteLine($"Read register {reg.RegisterAddress} failed for {meterName}: {ex.Message}");
                        }

                        // tiny settle gap
                        System.Threading.Thread.Sleep(5);
                    }
                }

                return reading;
            });
        }

        private object DecodeRegister(ushort[] registers, RegisterDataType dataType)
        {
            // default if null-ish - enum can't be null so use provided value
            if (registers == null || registers.Length == 0)
                return null;

            switch (dataType)
            {
                case RegisterDataType.Float:
                    if (registers.Length < 2)
                        return (float)registers[0];
                    {
                        // preserve previous behavior: use registers[1] as high, registers[0] as low
                        ushort high = registers.Length > 1 ? registers[1] : (ushort)0;
                        ushort low = registers[0];
                        return ModbusUtility.GetSingle(high, low);
                    }

                case RegisterDataType.Double:
                    {
                        var bytes = RegistersToBigEndianBytes(registers);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(bytes);
                        return BitConverter.ToDouble(bytes, 0);
                    }

                case RegisterDataType.Int16:
                    return (short)registers[0];

                case RegisterDataType.UInt16:
                    return registers[0];

                case RegisterDataType.Int32:
                    {
                        var regs = registers.Length >= 2 ? new[] { registers[0], registers[1] } : new[] { registers[0], (ushort)0 };
                        var bytes = RegistersToBigEndianBytes(regs);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(bytes);
                        return BitConverter.ToInt32(bytes, 0);
                    }

                case RegisterDataType.UInt32:
                    {
                        var regs = registers.Length >= 2 ? new[] { registers[0], registers[1] } : new[] { registers[0], (ushort)0 };
                        var bytes = RegistersToBigEndianBytes(regs);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(bytes);
                        return BitConverter.ToUInt32(bytes, 0);
                    }

                default:
                    // Fallback: return first register as ushort
                    return registers[0];
            }
        }

        // Build big-endian byte array from register array taking into account previous word ordering
        private byte[] RegistersToBigEndianBytes(ushort[] registers)
        {
            var bytes = new byte[registers.Length * 2];
            for (int i = 0; i < registers.Length; i++)
            {
                // previous float used GetSingle(registers[1], registers[0]) -> low word was at index 0
                // Construct bytes so the first pair corresponds to the highest-order register
                ushort reg = registers[registers.Length - 1 - i];
                bytes[i * 2] = (byte)(reg >> 8);        // high
                bytes[i * 2 + 1] = (byte)(reg & 0xFF);  // low
            }
            return bytes;
        }

        public async Task DisconnectAll()
        {
            foreach (var conn in _portConnections.Values)
                conn.Port?.Close();

            _portConnections.Clear();
            _meters.Clear();
            _meterConfigs.Clear();
        }

        public void Dispose() => DisconnectAll();
    }
}