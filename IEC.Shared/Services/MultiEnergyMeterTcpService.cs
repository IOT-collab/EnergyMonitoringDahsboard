using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using NModbus;
using NModbus.Utility;
using IEC.Shared.Models;

namespace IEC.Shared.Services
{
    // Modbus TCP implementation of IMultiEnergyMeterService responsibilities.
    // Mirrors the RTU service behavior but uses TCP sockets and Modbus IP masters.
    public class MultiEnergyMeterTcpService : IMultiEnergyMeterService
    {
        private class TcpConnection
        {
            public bool IsConnected { get; set; }
            public string Error { get; set; }
            public TcpClient Client;
            public IModbusMaster Master;
        }

        // keyed by "ip:port"
        private readonly Dictionary<string, TcpConnection> _connections = new();
        // keyed by meter name -> (connectionKey, slaveId)
        private readonly Dictionary<string, (string ConnectionKey, byte SlaveId)> _meters = new();
        // store full meters configuration (registers + comm)
        private readonly Dictionary<string, MetersConfig> _meterConfigs = new();
        private readonly object _lock = new();

        // Connection behavior
        private const int ConnectionTimeoutMs = 5000;
        private const int ConnectRetryDelayMs = 3000;

        // Configure expected to be called with meters using Protocol == ModbusTcp
        public async Task Configure(IEnumerable<MetersConfig> meters)
        {
            if (meters == null)
                return;

            foreach (var meter in meters)
            {
                if (string.IsNullOrWhiteSpace(meter?.MeterName))
                    continue;

                var comm = meter.Communication ?? new CommunicationConfig();

                // Skip non-TCP meters
                if ((comm.Protocol) != ProtocolsType.ModbusTcp)
                    continue;

                // store full config
                _meterConfigs[meter.MeterName] = meter;

                string ip = string.IsNullOrWhiteSpace(comm.IpAddress) ? "127.0.0.1" : comm.IpAddress;
                int port = comm.TcpPort > 0 ? comm.TcpPort : 502;
                byte slaveId = comm.SlaveId;

                string key = $"{ip}:{port}";
                _meters[meter.MeterName] = (key, slaveId);

                // If connection already exists and is connected, continue
                if (_connections.TryGetValue(key, out var existing))
                {
                    if (existing?.Client != null && existing.Client.Connected && existing.Master != null)
                    {
                        existing.IsConnected = true;
                        continue;
                    }

                    // Try reconnect existing entry (best-effort)
                    try
                    {
                        existing.Client?.Close();
                    }
                    catch { }

                    // Attempt to (re)connect using reliable routine
                    var reconnected = await TryConnectWithRetriesAsync(ip, port).ConfigureAwait(false);
                    _connections[key] = reconnected;
                    continue;
                }

                // create new connection using robust connect routine (with timeout + retry)
                var conn = await TryConnectWithRetriesAsync(ip, port).ConfigureAwait(false);
                _connections[key] = conn;
            }
        }

        // Attempts to connect repeatedly (in background) until a connection is established.
        // Uses Task.WhenAny to enforce a connect timeout per attempt (based on the user's proven pattern).
        private async Task<TcpConnection> TryConnectWithRetriesAsync(string ip, int port)
        {
            var result = new TcpConnection();

            while (true)
            {
                try
                {
                    var client = new TcpClient();

                    // Implement connection timeout using WhenAny
                    var connectTask = client.ConnectAsync(ip, port);
                    var completed = await Task.WhenAny(connectTask, Task.Delay(ConnectionTimeoutMs)).ConfigureAwait(false);
                    if (completed != connectTask)
                    {
                        // timed out
                        try { client.Close(); } catch { }
                        result.Client = null;
                        result.Master = null;
                        result.IsConnected = false;
                        result.Error = $"Connect timed out after {ConnectionTimeoutMs}ms.";
                        Console.WriteLine($"[TCP] Connect timeout to {ip}:{port}");
                        await Task.Delay(ConnectRetryDelayMs).ConfigureAwait(false);
                        continue; // retry
                    }

                    // Await to propagate exceptions if any
                    await connectTask.ConfigureAwait(false);

                    if (!client.Connected)
                    {
                        result.Client = null;
                        result.Master = null;
                        result.IsConnected = false;
                        result.Error = "TcpClient did not report connected after ConnectAsync.";
                        Console.WriteLine($"[TCP] Client not connected after ConnectAsync to {ip}:{port}");
                        try { client.Close(); } catch { }
                        await Task.Delay(ConnectRetryDelayMs).ConfigureAwait(false);
                        continue;
                    }

                    // Create master now that socket is connected
                    var factory = new ModbusFactory();
                    IModbusMaster master;
                    try
                    {
                        // prefer factory overload that accepts TcpClient if available
                        master = factory.CreateMaster(client);
                    }
                    catch (Exception ex)
                    {
                        // Unable to create an IP master for this TcpClient - cleanup and retry
                        result.Client = null;
                        result.Master = null;
                        result.IsConnected = false;
                        result.Error = $"Create master failed: {ex.Message}";
                        Console.WriteLine($"[TCP] Create master failed for {ip}:{port} → {ex.Message}");
                        try { client.Close(); } catch { }
                        await Task.Delay(ConnectRetryDelayMs).ConfigureAwait(false);
                        continue;
                    }

                    // Try to configure transport timeouts safely
                    try
                    {
                        master.Transport.ReadTimeout = 1000;
                        master.Transport.Retries = 2;
                    }
                    catch { /* ignore if transport not exposed */ }

                    result.Client = client;
                    result.Master = master;
                    result.IsConnected = client.Connected;
                    result.Error = null;

                    Console.WriteLine($"[TCP] Connected to {ip}:{port}");
                    return result;
                }
                catch (Exception ex)
                {
                    result.Client = null;
                    result.Master = null;
                    result.IsConnected = false;
                    result.Error = ex.Message;
                    Console.WriteLine($"[TCP] Connect error to {ip}:{port}: {ex.Message}. Retrying in {ConnectRetryDelayMs}ms");
                    try { await Task.Delay(ConnectRetryDelayMs).ConfigureAwait(false); } catch { }
                    // loop and retry
                }
            }
        }

        // Read all configured meters (those saved in _meterConfigs)
        public async Task<Dictionary<string, MeterReading>> ReadAllAsync()
        {
            var results = new Dictionary<string, MeterReading>();
            var meterNames = _meterConfigs.Keys.ToArray();

            foreach (var meterName in meterNames)
            {
                try
                {
                    var reading = await ReadOneAsync(meterName).ConfigureAwait(false);
                    results[meterName] = reading;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"TCP Read failed for {meterName}: {ex.Message}");
                    results[meterName] = new MeterReading { MeterName = meterName };
                }

                await Task.Delay(50).ConfigureAwait(false);
            }

            return results;
        }

        // Lookup stored registers and forward to the primary implementation
        public Task<MeterReading> ReadOneAsync(string meterName)
        {
            if (!_meterConfigs.TryGetValue(meterName, out var cfg) || cfg == null)
                throw new InvalidOperationException($"No saved configuration (registers) for meter '{meterName}'.");

            return ReadOneAsync(meterName, cfg.Registers);
        }

        // Primary implementation using provided RegisterConfig metadata
        public Task<MeterReading> ReadOneAsync(string meterName, IEnumerable<RegisterConfig> registers)
        {
            if (!_meters.TryGetValue(meterName, out var meter))
                throw new InvalidOperationException($"Meter '{meterName}' is not configured for TCP.");

            if (!_connections.TryGetValue(meter.ConnectionKey, out var conn) || conn?.Client == null || !conn.Client.Connected || conn.Master == null)
                throw new InvalidOperationException($"TCP connection for meter '{meterName}' is not available.");

            return Task.Run(() =>
            {
                var reading = new MeterReading { MeterName = meterName, Timestamp = DateTime.UtcNow };

                lock (_lock)
                {
                    foreach (var reg in registers.Where(r => r.IsEnabled))
                    {
                        try
                        {
                            int count = Math.Max(1, reg.Length);
                            ushort[] raw = conn.Master.ReadHoldingRegisters(meter.SlaveId, reg.RegisterAddress, (ushort)count);

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
                            Console.WriteLine($"TCP Read register {reg.RegisterAddress} failed for {meterName}: {ex.Message}");
                        }

                        System.Threading.Thread.Sleep(5);
                    }
                }

                return reading;
            });
        }

        private object DecodeRegister(ushort[] registers, RegisterDataType dataType)
        {
            if (registers == null || registers.Length == 0)
                return null;

            switch (dataType)
            {
                case RegisterDataType.Float:
                    if (registers.Length < 2)
                        return (float)registers[0];
                    {
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
                    return registers[0];
            }
        }

        private byte[] RegistersToBigEndianBytes(ushort[] registers)
        {
            var bytes = new byte[registers.Length * 2];
            for (int i = 0; i < registers.Length; i++)
            {
                ushort reg = registers[registers.Length - 1 - i];
                bytes[i * 2] = (byte)(reg >> 8);
                bytes[i * 2 + 1] = (byte)(reg & 0xFF);
            }
            return bytes;
        }

        public async Task DisconnectAll()
        {
            foreach (var conn in _connections.Values)
            {
                try
                {
                    conn.Master?.Dispose();
                }
                catch { }

                try
                {
                    conn.Client?.Close();
                }
                catch { }
            }

            _connections.Clear();
            _meters.Clear();
            _meterConfigs.Clear();

            await Task.CompletedTask;
        }

        public void Dispose() => DisconnectAll().ConfigureAwait(false).GetAwaiter().GetResult();

        // Expose HasMeter for coordinator usage
        public bool HasMeter(string meterName)
        {
            if (string.IsNullOrWhiteSpace(meterName))
                return false;

            return _meterConfigs.ContainsKey(meterName) || _meters.ContainsKey(meterName);
        }
    }
}