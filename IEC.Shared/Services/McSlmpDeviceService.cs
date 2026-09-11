using IEC.Shared.Models;
using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace IEC.Shared.Services;

public sealed class McSlmpDeviceService : IDisposable
{
    private readonly Dictionary<string, MetersConfig> _meters = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _ioLock = new(1, 1);
    private const int ConnectTimeoutMs = 3000;
    private const ushort MonitoringTimer = 10;

    private static readonly IReadOnlyDictionary<McDeviceType, byte> DeviceCodes =
        new Dictionary<McDeviceType, byte>
        {
            [McDeviceType.D] = 0xA8, [McDeviceType.S] = 0x98, [McDeviceType.X] = 0x9C, [McDeviceType.Y] = 0x9D,
            [McDeviceType.M] = 0x90, [McDeviceType.L] = 0x92, [McDeviceType.T] = 0xC1, [McDeviceType.C] = 0xC5
        };

    public Task Configure(IEnumerable<MetersConfig> meters)
    {
        _meters.Clear();
        foreach (var meter in meters ?? Array.Empty<MetersConfig>())
        {
            if (meter == null || string.IsNullOrWhiteSpace(meter.MeterName)) continue;
            _meters[meter.MeterName] = meter;
        }
        return Task.CompletedTask;
    }

    public async Task<Dictionary<string, MeterReading>> ReadAllAsync()
    {
        var result = new Dictionary<string, MeterReading>(StringComparer.OrdinalIgnoreCase);
        foreach (var meter in _meters.Values) result[meter.MeterName] = await ReadMeterAsync(meter).ConfigureAwait(false);
        return result;
    }

    public Task<MeterReading> ReadOneAsync(string meterName)
    {
        if (!_meters.TryGetValue(meterName ?? string.Empty, out var meter))
            throw new InvalidOperationException($"MC/SLMP meter '{meterName}' is not configured.");
        return ReadMeterAsync(meter);
    }

    public bool HasMeter(string meterName) => !string.IsNullOrWhiteSpace(meterName) && _meters.ContainsKey(meterName);

    public async Task WriteAsync(string meterName, string parameterName, object value, CancellationToken cancellation = default)
    {
        if (!_meters.TryGetValue(meterName ?? string.Empty, out var meter))
            throw new InvalidOperationException($"MC/SLMP meter '{meterName}' is not configured.");
        var mapping = meter.Registers?.FirstOrDefault(x => string.Equals(x.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase));
        if (mapping == null) throw new InvalidOperationException($"MC/SLMP mapping '{parameterName}' was not found.");
        if (mapping.McAccess == McAccessMode.Read) throw new InvalidOperationException($"Mapping '{parameterName}' is configured as read-only.");

        await _ioLock.WaitAsync(cancellation).ConfigureAwait(false);
        try
        {
            var code = DeviceCode(mapping.McDevice);
            byte[] response = IsBit(mapping)
                ? await Send(meter, BuildWriteBits(mapping.RegisterAddress, code, new[] { (byte)(ToBool(value) ? 1 : 0) }), cancellation).ConfigureAwait(false)
                : await Send(meter, BuildWriteWords(mapping.RegisterAddress, code, Encode(mapping, value)), cancellation).ConfigureAwait(false);
            Validate(response, "write");
        }
        finally { _ioLock.Release(); }
    }

    public Task DisconnectAll() => Task.CompletedTask;

    private async Task<MeterReading> ReadMeterAsync(MetersConfig meter)
    {
        var reading = new MeterReading { MeterName = meter.MeterName, Timestamp = DateTime.UtcNow };
        var maps = (meter.Registers ?? new()).Where(x => x.IsEnabled && x.McAccess != McAccessMode.Write).ToList();
        if (maps.Count == 0) { reading.CommunicationError = "No enabled MC/SLMP read mappings are configured."; return reading; }
        try
        {
            await _ioLock.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var map in maps)
                {
                    var code = DeviceCode(map.McDevice);
                    if (IsBit(map))
                    {
                        var bits = await ReadBits(meter, map.RegisterAddress, code, Math.Max(1, map.Length)).ConfigureAwait(false);
                        if (bits.Length > 0) reading.Values[map.ParameterName] = bits[0] != 0;
                    }
                    else
                    {
                        var words = await ReadWords(meter, map.RegisterAddress, code, Math.Max(1, map.Length)).ConfigureAwait(false);
                        if (words.Length > 0) reading.Values[map.ParameterName] = Decode(map, words);
                    }
                }
            }
            finally { _ioLock.Release(); }
        }
        catch (Exception ex) { reading.CommunicationError = ex.Message; }
        return reading;
    }

    private async Task<short[]> ReadWords(MetersConfig meter, int address, byte code, int points)
    {
        var response = await Send(meter, BuildRead(address, code, points, false), CancellationToken.None).ConfigureAwait(false);
        Validate(response, "read words");
        var count = Math.Min(points, Math.Max(0, (response.Length - 11) / 2));
        var result = new short[count];
        for (var i = 0; i < count; i++) result[i] = BinaryPrimitives.ReadInt16LittleEndian(response.AsSpan(11 + i * 2, 2));
        return result;
    }

    private async Task<byte[]> ReadBits(MetersConfig meter, int address, byte code, int points)
    {
        var response = await Send(meter, BuildRead(address, code, points, true), CancellationToken.None).ConfigureAwait(false);
        Validate(response, "read bits");
        var count = Math.Min(points, Math.Max(0, response.Length - 11));
        var result = new byte[count];
        Array.Copy(response, 11, result, 0, count);
        return result;
    }

    private static async Task<byte[]> Send(MetersConfig meter, byte[] command, CancellationToken cancellation)
    {
        var comm = meter.Communication ?? new CommunicationConfig();
        var host = comm.IpAddress?.Trim();
        if (string.IsNullOrWhiteSpace(host)) throw new InvalidOperationException("MC/SLMP IP address is not configured.");
        if (comm.TcpPort is < 1 or > 65535) throw new InvalidOperationException("MC/SLMP TCP port is invalid.");

        using var tcp = new TcpClient();
        var connect = tcp.ConnectAsync(host, comm.TcpPort);
        var timeout = Task.Delay(ConnectTimeoutMs, cancellation);
        if (await Task.WhenAny(connect, timeout).ConfigureAwait(false) == timeout)
            throw new TimeoutException($"MC/SLMP connection to {host}:{comm.TcpPort} timed out.");
        await connect.ConfigureAwait(false);

        using var stream = tcp.GetStream();
        stream.ReadTimeout = ConnectTimeoutMs; stream.WriteTimeout = ConnectTimeoutMs;
        await stream.WriteAsync(command, cancellation).ConfigureAwait(false);
        var header = new byte[11]; await ReadExact(stream, header, cancellation).ConfigureAwait(false);
        var responseLength = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(7, 2));
        var payload = new byte[Math.Max(0, responseLength - 2)];
        if (payload.Length > 0) await ReadExact(stream, payload, cancellation).ConfigureAwait(false);
        var response = new byte[header.Length + payload.Length];
        Buffer.BlockCopy(header, 0, response, 0, header.Length); Buffer.BlockCopy(payload, 0, response, header.Length, payload.Length);
        return response;
    }

    private static async Task ReadExact(NetworkStream stream, byte[] buffer, CancellationToken cancellation)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellation).ConfigureAwait(false);
            if (read == 0) throw new IOException("PLC closed the MC/SLMP connection before the response was complete.");
            offset += read;
        }
    }

    private static void Validate(byte[] response, string operation)
    {
        if (response.Length < 11) throw new IOException($"Incomplete MC/SLMP response during {operation}.");
        var endCode = BinaryPrimitives.ReadUInt16LittleEndian(response.AsSpan(9, 2));
        if (endCode != 0) throw new IOException($"PLC MC/SLMP error 0x{endCode:X4} during {operation}.");
    }

    private static byte DeviceCode(McDeviceType type) => DeviceCodes.TryGetValue(type, out var code) ? code :
        throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported MC device type.");

    private static bool IsBit(RegisterConfig map) =>
        map.McDevice is McDeviceType.X or McDeviceType.Y or McDeviceType.M or McDeviceType.L || map.DataType == RegisterDataType.Bool;

    private static object Decode(RegisterConfig map, short[] words)
    {
        object value = map.DataType switch
        {
            RegisterDataType.Int16 => words[0],
            RegisterDataType.UInt16 => (ushort)words[0],
            RegisterDataType.Int32 => words.Length >= 2 ? words[0] | (words[1] << 16) : words[0],
            RegisterDataType.UInt32 => words.Length >= 2 ? (uint)((ushort)words[0] | (uint)((ushort)words[1] << 16)) : (ushort)words[0],
            RegisterDataType.Float => words.Length >= 2 ? BitConverter.Int32BitsToSingle(words[0] | (words[1] << 16)) : (float)words[0],
            RegisterDataType.Double => words.Length >= 4 ? BitConverter.Int64BitsToDouble((long)(uint)(words[0] | (words[1] << 16)) | ((long)(uint)(words[2] | (words[3] << 16)) << 32)) : (double)words[0],
            RegisterDataType.AsciiString => Encoding.ASCII.GetString(words.SelectMany(BitConverter.GetBytes).ToArray()).TrimEnd('\0'),
            _ => words[0]
        };
        if (value is IConvertible && value is not string && map.ScaleFactor != 1)
            return Convert.ToDouble(value, CultureInfo.InvariantCulture) * map.ScaleFactor;
        return value;
    }

    private static short[] Encode(RegisterConfig map, object value)
    {
        var words = new short[Math.Max(1, map.Length)];
        switch (map.DataType)
        {
            case RegisterDataType.Float:
                var f = Convert.ToSingle(value, CultureInfo.InvariantCulture) / (map.ScaleFactor == 0 ? 1 : map.ScaleFactor);
                var bits = BitConverter.SingleToInt32Bits(f); words[0] = (short)(bits & 0xFFFF); if (words.Length > 1) words[1] = (short)(bits >> 16); break;
            case RegisterDataType.Int32:
            case RegisterDataType.UInt32:
                var integer = Convert.ToInt32(value, CultureInfo.InvariantCulture); words[0] = (short)(integer & 0xFFFF); if (words.Length > 1) words[1] = (short)(integer >> 16); break;
            default: words[0] = Convert.ToInt16(value, CultureInfo.InvariantCulture); break;
        }
        return words;
    }

    private static bool ToBool(object value) => value is bool b ? b : Convert.ToDouble(value, CultureInfo.InvariantCulture) != 0;

    private static byte[] BuildRead(int address, byte code, int points, bool bitUnit)
    {
        points = Math.Clamp(points, 1, ushort.MaxValue);
        using var ms = new MemoryStream();
        Header(ms, 12); U16(ms, MonitoringTimer); U16(ms, 0x0401); U16(ms, bitUnit ? (ushort)1 : (ushort)0);
        Device(ms, address, code); U16(ms, (ushort)points); return ms.ToArray();
    }

    private static byte[] BuildWriteWords(int address, byte code, short[] values)
    {
        using var ms = new MemoryStream();
        Header(ms, (ushort)(12 + values.Length * 2)); U16(ms, MonitoringTimer); U16(ms, 0x1401); U16(ms, 0);
        Device(ms, address, code); U16(ms, (ushort)values.Length);
        foreach (var value in values) U16(ms, unchecked((ushort)value));
        return ms.ToArray();
    }

    private static byte[] BuildWriteBits(int address, byte code, byte[] bits)
    {
        using var ms = new MemoryStream();
        Header(ms, (ushort)(12 + bits.Length)); U16(ms, MonitoringTimer); U16(ms, 0x1401); U16(ms, 1);
        Device(ms, address, code); U16(ms, (ushort)bits.Length); ms.Write(bits, 0, bits.Length); return ms.ToArray();
    }

    private static void Header(Stream stream, ushort length)
    {
        stream.WriteByte(0x50); stream.WriteByte(0x00); stream.WriteByte(0x00); stream.WriteByte(0xFF);
        stream.WriteByte(0xFF); stream.WriteByte(0x03); stream.WriteByte(0x00); U16(stream, length);
    }
    private static void Device(Stream stream, int address, byte code)
    {
        stream.WriteByte((byte)address); stream.WriteByte((byte)(address >> 8)); stream.WriteByte((byte)(address >> 16)); stream.WriteByte(code);
    }
    private static void U16(Stream stream, ushort value) { stream.WriteByte((byte)value); stream.WriteByte((byte)(value >> 8)); }

    public void Dispose() => _ioLock.Dispose();
}