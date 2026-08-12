using IEC.Shared.Models;
using NModbus;
using NModbus.Serial;
using NModbus.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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

            // Reconfiguration must start from a clean bus. Otherwise stale meter
            // mappings and a previously opened SerialPort/master remain active.
            await DisconnectAll().ConfigureAwait(false);

            var meterList = meters
                .Where(m => m != null && !string.IsNullOrWhiteSpace(m.MeterName))
                .ToList();

            // Every slave sharing a physical RTU port must use identical serial settings.
            foreach (var portGroup in meterList.GroupBy(m => m.Communication?.ComPort ?? "COM1", StringComparer.OrdinalIgnoreCase))
            {
                // Recover a transient/null parity from another meter on the same
                // physical bus. The configuration UI may previously have written
                // null while changing ComboBox selections, although JSON is valid.
                var sharedParity = portGroup
                    .Select(m => m.Communication?.Parity)
                    .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p)) ?? "Even";

                foreach (var groupedMeter in portGroup)
                {
                    groupedMeter.Communication ??= new CommunicationConfig();
                    if (string.IsNullOrWhiteSpace(groupedMeter.Communication.Parity))
                        groupedMeter.Communication.Parity = sharedParity;
                }

                var first = portGroup.First().Communication ?? new CommunicationConfig();
                if (portGroup.Skip(1).Any(m =>
                {
                    var comm = m.Communication ?? new CommunicationConfig();
                    return comm.BaudRate != first.BaudRate ||
                           comm.DataBits != first.DataBits ||
                           comm.StopBits != first.StopBits ||
                           !string.Equals(comm.Parity, first.Parity, StringComparison.OrdinalIgnoreCase);
                }))
                {
                    throw new InvalidOperationException(
                        $"Meters on {portGroup.Key} must use the same baud rate, parity, data bits and stop bits.");
                }

                var duplicateSlave = portGroup
                    .GroupBy(m => m.Communication?.SlaveId ?? (byte)1)
                    .FirstOrDefault(g => g.Count() > 1);
                if (duplicateSlave != null)
                {
                    throw new InvalidOperationException(
                        $"Slave ID {duplicateSlave.Key} is configured more than once on {portGroup.Key}.");
                }
            }

            foreach (var meter in meterList)
            {
                if (string.IsNullOrWhiteSpace(meter?.MeterName))
                    continue;

                // store full config for later use (registers + comm)
                _meterConfigs[meter.MeterName] = meter;

                // Log the incoming meter object (helps verify what caller passed)
                try
                {
                    var serOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    serOpts.Converters.Add(new JsonStringEnumConverter());
                    Console.WriteLine($"Configure() received meter: {JsonSerializer.Serialize(meter, serOpts)}");
                }
                catch { /* best-effort logging, ignore if serialization fails */ }

                var comm = meter.Communication ?? new CommunicationConfig();

                // Defensive parity handling + logging so we can see why it's null
                var parity = System.IO.Ports.Parity.Even; // choose a safe default
                if (string.IsNullOrWhiteSpace(comm.Parity))
                {
                    Console.WriteLine($"Warning: meter '{meter?.MeterName}' has null/empty Communication.Parity. Defaulting to Even.");
                }
                else if (!Enum.TryParse<System.IO.Ports.Parity>(comm.Parity, true, out var parsedParity))
                {
                    Console.WriteLine($"Warning: meter '{meter?.MeterName}' parity '{comm.Parity}' not recognized. Defaulting to Even.");
                }
                else
                {
                    parity = parsedParity;
                }

                string portName = comm.ComPort ?? "COM1";
                int baud = comm.BaudRate;
                byte slaveId = comm.SlaveId;

                _meters[meter.MeterName] = (portName, slaveId);

                // Open the physical port once per unique PortName (shared across meters on that bus)
                if (!_portConnections.ContainsKey(portName))
                {
                   // var parity = System.IO.Ports.Parity.Even;
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
                        StopBits = stopBits,
                        Handshake = Handshake.None,
                        RtsEnable = false,
                        DtrEnable = false,
                        ReadTimeout = 2000,
                        WriteTimeout = 2000,
                        ReadBufferSize = 4096,
                        WriteBufferSize = 2048
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

                    master.Transport.ReadTimeout = 2000;
                    master.Transport.WriteTimeout = 2000;
                    // One retry is sufficient on a local RS-485 bus. Large retry
                    // counts multiply the delay for every register of an offline slave.
                    master.Transport.Retries = 1;
                    master.Transport.WaitToRetryMilliseconds = 150;

                    _portConnections[portName] = new PortConnection
                    {
                        Port = port,
                        Master = master,
                        IsConnected = port.IsOpen
                    };

                    // USB/RS-485 converters and some meters need a short quiet
                    // period after the port is opened before the first request.
                    port.DiscardInBuffer();
                    port.DiscardOutBuffer();
                    await Task.Delay(500).ConfigureAwait(false);
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
                            //MessageBox.Show($"Unable to Connect: {ex.Message}");
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

                // Give the shared two-wire bus and converter direction control
                // time to settle before addressing a different slave.
                await Task.Delay(200).ConfigureAwait(false);
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

                // Determine per-meter word order (default LowHigh)
                var wordOrder = RegisterWordOrder.LowHigh;
                if (_meterConfigs.TryGetValue(meterName, out var meterCfg) && meterCfg?.Communication != null)
                {
                    wordOrder = meterCfg.Communication.WordOrder;
                }

                lock (_lock)
                {
                    foreach (var reg in registers.Where(r => r.IsEnabled))
                    {
                        try
                        {
                            // Length is number of registers to read (clamp to Modbus limit 125)
                            int count = Math.Max(Math.Max(1, reg.Length), RequiredRegisterCount(reg.DataType));
                            if (count > 125) count = 125;

                            ushort[] raw = null;
                            bool[] rawBits = null;

                            // Helper to attempt a read and return true on success
                            bool TryRead(ushort startAddress, ushort readCount, out ushort[] result, out string failure)
                            {
                                result = null!;
                                failure = null!;
                                try
                                {
                                    result = reg.DataArea == ModbusDataArea.InputRegister
                                        ? conn.Master.ReadInputRegisters(meter.SlaveId, startAddress, readCount)
                                        : conn.Master.ReadHoldingRegisters(meter.SlaveId, startAddress, readCount);
                                    return true;
                                }
                                catch (NModbus.SlaveException sex)
                                {
                                    failure = $"SlaveException: FunctionCode={sex.FunctionCode}, ExceptionCode={sex.SlaveExceptionCode}";
                                    return false;
                                }
                                catch (Exception ex)
                                {
                                    failure = $"Exception: {ex.Message}";
                                    return false;
                                }
                            }

                            bool TryReadBits(ushort startAddress, ushort readCount, out bool[] result, out string failure)
                            {
                                result = null!;
                                failure = null!;
                                try
                                {
                                    result = reg.DataArea == ModbusDataArea.Coil
                                        ? conn.Master.ReadCoils(meter.SlaveId, startAddress, readCount)
                                        : conn.Master.ReadInputs(meter.SlaveId, startAddress, readCount);
                                    return true;
                                }
                                catch (NModbus.SlaveException sex)
                                {
                                    failure = $"SlaveException: FunctionCode={sex.FunctionCode}, ExceptionCode={sex.SlaveExceptionCode}";
                                    return false;
                                }
                                catch (Exception ex)
                                {
                                    failure = $"Exception: {ex.Message}";
                                    return false;
                                }
                            }

                            // Candidate start addresses to try:
                            // 1) as configured
                            // 2) if configured looks like 40001-style (>=40001) try subtracting 40001
                            // 3) if configured looks like 30001-style (>=30001) try subtracting 30001
                            // (use unique list so we don't duplicate attempts)
                            var candidates = new List<int> { reg.RegisterAddress };

                            if (reg.RegisterAddress >= 40001)
                                candidates.Add(reg.RegisterAddress - 40001);
                            if (reg.RegisterAddress >= 30001)
                                candidates.Add(reg.RegisterAddress - 30001);

                            // also try a zero-based variant if the configured value looks like 1-based (>=1)
                            if (reg.RegisterAddress >= 1)
                                candidates.Add(reg.RegisterAddress - 1);

                            // ensure unique and non-negative, and fit into ushort
                            var uniqueCandidates = candidates.Distinct()
                                .Where(a => a >= 0 && a <= ushort.MaxValue)
                                .Select(a => (ushort)a)
                                .ToArray();

                            string lastFailure = null;
                            foreach (var start in uniqueCandidates)
                            {
                                var isBitArea = reg.DataArea == ModbusDataArea.Coil || reg.DataArea == ModbusDataArea.DiscreteInput;
                                if (isBitArea)
                                {
                                    if (TryReadBits(start, (ushort)count, out var attemptBits, out var bitFailure))
                                    {
                                        rawBits = attemptBits;
                                        break;
                                    }
                                    lastFailure = bitFailure;
                                }
                                else
                                {
                                    if (TryRead(start, (ushort)count, out var attemptRaw, out var registerFailure))
                                    {
                                        raw = attemptRaw;
                                        break;
                                    }
                                    lastFailure = registerFailure;
                                }
                                // small settle between attempts
                                System.Threading.Thread.Sleep(10);
                            }

                            if (raw == null && rawBits == null)
                            {
                                // If a slave does not answer the first requested register,
                                // do not repeat the same timeout for every remaining register.
                                var noResponseKey = string.IsNullOrWhiteSpace(reg.ParameterName)
                                    ? reg.RegisterAddress.ToString()
                                    : reg.ParameterName;
                                reading.Values[noResponseKey] = null;
                                reading.CommunicationError = SimplifyCommunicationError(lastFailure);
                                Console.WriteLine(
                                    $"No RTU response: meter='{meterName}', slave={meter.SlaveId}, register={reg.RegisterAddress}. {lastFailure}");
                                break;
                            }

                            // If device uses HighLow, swap words into the format expected by decoder
                            ushort[] rawForDecode = raw;
                            if (raw != null && raw.Length >= 2 && wordOrder == RegisterWordOrder.HighLow)
                            {
                                rawForDecode = new ushort[raw.Length];
                                for (int i = 0; i < raw.Length; i += 2)
                                {
                                    if (i + 1 < raw.Length)
                                    {
                                        rawForDecode[i] = raw[i + 1];
                                        rawForDecode[i + 1] = raw[i];
                                    }
                                    else
                                    {
                                        rawForDecode[i] = raw[i];
                                    }
                                }
                            }

                            // Use enum-typed DataType
                            object value = rawBits != null
                                ? DecodeBits(rawBits, reg.DataType)
                                : DecodeRegister(rawForDecode, reg.DataType);

                            // apply scale factor
                            value = ApplyScale(value, reg.ScaleFactor);

                            var key = string.IsNullOrWhiteSpace(reg.ParameterName) ? reg.RegisterAddress.ToString() : reg.ParameterName;
                            reading.Values[key] = value;
                        }
                        catch (NModbus.SlaveException sex)
                        {
                            var key = string.IsNullOrWhiteSpace(reg.ParameterName) ? reg.RegisterAddress.ToString() : reg.ParameterName;
                            reading.Values[key] = null;
                            Console.WriteLine($"SlaveException reading {reg.RegisterAddress} for {meterName}: Function={sex.FunctionCode}, Code={sex.InnerException}, Message={sex.Message}");
                        }
                        catch (Exception ex)
                        {
                            var key = string.IsNullOrWhiteSpace(reg.ParameterName) ? reg.RegisterAddress.ToString() : reg.ParameterName;
                            reading.Values[key] = null;
                            Console.WriteLine($"Read register {reg.RegisterAddress} failed for {meterName}: {ex.Message}");
                        }

                        // tiny settle gap
                        // Conservative RTU inter-frame gap for field devices and
                        // auto-direction USB/RS-485 converters.
                        System.Threading.Thread.Sleep(40);
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
                case RegisterDataType.Bool:
                    return registers[0] != 0;
                case RegisterDataType.Byte:
                    return (byte)(registers[0] & 0xFF);
                case RegisterDataType.SByte:
                    return unchecked((sbyte)(registers[0] & 0xFF));
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

                case RegisterDataType.Int64:
                    return BitConverter.ToInt64(ToHostBytes(registers, 4), 0);
                case RegisterDataType.UInt64:
                    return BitConverter.ToUInt64(ToHostBytes(registers, 4), 0);
                case RegisterDataType.AsciiString:
                    return Encoding.ASCII.GetString(registers.SelectMany(r => new[] { (byte)(r >> 8), (byte)r }).ToArray()).TrimEnd('\0', ' ');

                default:
                    // Fallback: return first register as ushort
                    return registers[0];
            }
        }

        private static object DecodeBits(bool[] bits, RegisterDataType dataType)
        {
            if (bits == null || bits.Length == 0) return false;
            if (dataType == RegisterDataType.AsciiString) return string.Join(",", bits.Select(b => b ? "1" : "0"));
            ulong packed = 0;
            for (var i = 0; i < Math.Min(bits.Length, 64); i++) if (bits[i]) packed |= 1UL << i;
            return dataType switch
            {
                RegisterDataType.Bool => bits[0],
                RegisterDataType.Byte => (byte)packed,
                RegisterDataType.SByte => unchecked((sbyte)packed),
                RegisterDataType.Int16 => unchecked((short)packed),
                RegisterDataType.UInt16 => (ushort)packed,
                RegisterDataType.Int32 => unchecked((int)packed),
                RegisterDataType.UInt32 => (uint)packed,
                RegisterDataType.Int64 => unchecked((long)packed),
                RegisterDataType.UInt64 => packed,
                _ => bits[0]
            };
        }

        private static int RequiredRegisterCount(RegisterDataType type) => type switch
        {
            RegisterDataType.Int64 or RegisterDataType.UInt64 or RegisterDataType.Double => 4,
            RegisterDataType.Int32 or RegisterDataType.UInt32 or RegisterDataType.Float => 2,
            _ => 1
        };

        private static object ApplyScale(object value, float scale)
        {
            if (value == null || value is bool || value is string || scale == 1f) return value;
            return Convert.ToDouble(value) * scale;
        }

        private byte[] ToHostBytes(ushort[] registers, int requiredRegisters)
        {
            var padded = registers.Concat(Enumerable.Repeat((ushort)0, requiredRegisters)).Take(requiredRegisters).ToArray();
            var bytes = RegistersToBigEndianBytes(padded);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return bytes;
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
            lock (_lock)
            {
                foreach (var conn in _portConnections.Values)
                {
                    try { conn.Master?.Dispose(); } catch { }
                    try { conn.Port?.Close(); } catch { }
                    try { conn.Port?.Dispose(); } catch { }
                }

                _portConnections.Clear();
                _meters.Clear();
                _meterConfigs.Clear();
            }

            await Task.CompletedTask;
        }

        private static string SimplifyCommunicationError(string failure)
        {
            if (string.IsNullOrWhiteSpace(failure))
                return "No response";

            if (failure.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                failure.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Timeout";

            if (failure.IndexOf("checksum", StringComparison.OrdinalIgnoreCase) >= 0 ||
                failure.IndexOf("CRC", StringComparison.OrdinalIgnoreCase) >= 0)
                return "CRC/frame error";

            if (failure.IndexOf("SlaveException", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Modbus exception";

            return failure.Length > 80 ? failure.Substring(0, 80) : failure;
        }

        public void Dispose() => DisconnectAll().ConfigureAwait(false).GetAwaiter().GetResult();

        public Task WriteCoilAsync(string meterName, ushort address, bool value) => Task.Run(() =>
        {
            var target = GetWritableConnection(meterName);
            lock (_lock) target.Master.WriteSingleCoil(target.SlaveId, address, value);
        });

        public Task WriteRegisterAsync(string meterName, ushort address, ushort value) => Task.Run(() =>
        {
            var target = GetWritableConnection(meterName);
            lock (_lock) target.Master.WriteSingleRegister(target.SlaveId, address, value);
        });

        public async Task<bool> ReadBooleanAsync(string meterName, ModbusDataArea area, ushort address)
        {
            var target = GetWritableConnection(meterName);
            return await Task.Run(() =>
            {
                lock (_lock) return (bool)(area switch
                {
                    ModbusDataArea.Coil => target.Master.ReadCoils(target.SlaveId, address, 1)[0],
                    ModbusDataArea.DiscreteInput => target.Master.ReadInputs(target.SlaveId, address, 1)[0],
                    ModbusDataArea.InputRegister => target.Master.ReadInputRegisters(target.SlaveId, address, 1)[0] != 0,
                    _ => target.Master.ReadHoldingRegisters(target.SlaveId, address, 1)[0] != 0
                });
            }).ConfigureAwait(false);
        }

        private (byte SlaveId, IModbusMaster Master) GetWritableConnection(string meterName)
        {
            if (!_meters.TryGetValue(meterName, out var meter) ||
                !_portConnections.TryGetValue(meter.PortName, out var connection) ||
                connection?.Master == null || connection.Port == null || !connection.Port.IsOpen)
                throw new InvalidOperationException($"RTU device '{meterName}' is not connected.");
            return (meter.SlaveId, connection.Master);
        }
    }
}
