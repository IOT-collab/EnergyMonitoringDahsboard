using IEC.Shared.Models;
using S7.Net;
using System.Globalization;
using System.Text;

namespace IEC.Shared.Services;

/// <summary>
/// Siemens S7 tag transport over the PLC's PROFINET Ethernet interface.
/// This uses S7 communication (ISO-on-TCP, port 102), not a real-time
/// PROFINET-IO controller stack. RegisterConfig.Address stores S7.Net
/// variable syntax such as DB1.DBX0.0, DB1.DBW2, or DB1.DBD4.
/// </summary>
public sealed class S7ProfinetDeviceService : IDisposable
{
    private readonly Dictionary<string, MetersConfig> _meters = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _ioLock = new(1, 1);

    public Task Configure(IEnumerable<MetersConfig> meters)
    {
        _meters.Clear();
        foreach (var meter in meters ?? Array.Empty<MetersConfig>())
        {
            if (meter != null && !string.IsNullOrWhiteSpace(meter.MeterName))
                _meters[meter.MeterName] = meter;
        }
        return Task.CompletedTask;
    }

    public async Task<Dictionary<string, MeterReading>> ReadAllAsync()
    {
        var result = new Dictionary<string, MeterReading>(StringComparer.OrdinalIgnoreCase);
        foreach (var meter in _meters.Values)
            result[meter.MeterName] = await ReadMeterAsync(meter).ConfigureAwait(false);
        return result;
    }

    public Task<MeterReading> ReadOneAsync(string meterName)
    {
        if (!_meters.TryGetValue(meterName ?? string.Empty, out var meter))
            throw new InvalidOperationException($"PROFINET/S7 meter '{meterName}' is not configured.");
        return ReadMeterAsync(meter);
    }

    public bool HasMeter(string meterName) =>
        !string.IsNullOrWhiteSpace(meterName) && _meters.ContainsKey(meterName);

    public async Task WriteAsync(string meterName, string address, object value, RegisterDataType dataType)
    {
        if (!_meters.TryGetValue(meterName ?? string.Empty, out var meter))
            throw new InvalidOperationException($"PROFINET/S7 meter '{meterName}' is not configured.");
        if (string.IsNullOrWhiteSpace(address))
            throw new InvalidOperationException("An S7 address is required for a write.");
        var communication = meter.Communication ?? new CommunicationConfig();
        if (string.IsNullOrWhiteSpace(communication.IpAddress))
            throw new InvalidOperationException("PROFINET/S7 IP address is not configured.");
        if (communication.S7Rack is < 0 or > short.MaxValue || communication.S7Slot is < 0 or > short.MaxValue)
            throw new InvalidOperationException("S7 rack and slot must be non-negative values.");

        await _ioLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var plc = new Plc(ToCpuType(communication.S7Cpu), communication.IpAddress.Trim(),
                (short)communication.S7Rack, (short)communication.S7Slot);
            try
            {
                plc.Open();
                plc.Write(address.Trim(), Encode(value, dataType));
            }
            finally
            {
                try { plc.Close(); } catch { }
            }
        }
        finally { _ioLock.Release(); }
    }

    public Task DisconnectAll() => Task.CompletedTask;

    public void Dispose()
    {
        _meters.Clear();
        _ioLock.Dispose();
    }

    private async Task<MeterReading> ReadMeterAsync(MetersConfig meter)
    {
        var reading = new MeterReading { MeterName = meter.MeterName, Timestamp = DateTime.UtcNow };
        var mappings = (meter.Registers ?? new())
            .Where(x => x.IsEnabled && !string.IsNullOrWhiteSpace(x.Address))
            .ToList();
        if (mappings.Count == 0)
        {
            reading.CommunicationError = "No enabled S7 address mappings are configured.";
            return reading;
        }

        var communication = meter.Communication ?? new CommunicationConfig();
        if (string.IsNullOrWhiteSpace(communication.IpAddress))
        {
            reading.CommunicationError = "PROFINET/S7 IP address is not configured.";
            return reading;
        }
        if (communication.S7Rack is < 0 or > short.MaxValue || communication.S7Slot is < 0 or > short.MaxValue)
        {
            reading.CommunicationError = "S7 rack and slot must be non-negative values.";
            return reading;
        }

        await _ioLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var plc = new Plc(ToCpuType(communication.S7Cpu), communication.IpAddress.Trim(),
                (short)communication.S7Rack, (short)communication.S7Slot);
            try
            {
                plc.Open();
                foreach (var mapping in mappings)
                {
                    var key = string.IsNullOrWhiteSpace(mapping.ParameterName)
                        ? mapping.Address!.Trim()
                        : mapping.ParameterName;
                    try
                    {
                        var raw = plc.Read(mapping.Address!.Trim());
                        reading.Values[key] = ApplyScale(Decode(raw, mapping.DataType), mapping.ScaleFactor);
                    }
                    catch (Exception ex)
                    {
                        reading.Values[key] = null!;
                        reading.CommunicationError = AppendError(reading.CommunicationError,
                            $"{key}: {ex.Message}");
                    }
                }
            }
            finally
            {
                try { plc.Close(); } catch { }
            }
        }
        catch (Exception ex)
        {
            reading.CommunicationError = AppendError(reading.CommunicationError,
                $"{communication.IpAddress}:{S7Port}: {ex.Message}");
        }
        finally
        {
            _ioLock.Release();
        }

        return reading;
    }

    private static CpuType ToCpuType(S7CpuType cpu) => cpu switch
    {
        S7CpuType.S7200 => CpuType.S7200,
        S7CpuType.S7300 => CpuType.S7300,
        S7CpuType.S7400 => CpuType.S7400,
        S7CpuType.S71500 => CpuType.S71500,
        _ => CpuType.S71200
    };

    private static object? Decode(object? raw, RegisterDataType dataType)
    {
        if (raw == null) return null;
        return dataType switch
        {
            RegisterDataType.Bool => raw is bool b ? b : Convert.ToBoolean(raw, CultureInfo.InvariantCulture),
            RegisterDataType.Byte => Convert.ToByte(raw, CultureInfo.InvariantCulture),
            RegisterDataType.SByte => Convert.ToSByte(raw, CultureInfo.InvariantCulture),
            RegisterDataType.Int16 => Convert.ToInt16(raw, CultureInfo.InvariantCulture),
            RegisterDataType.UInt16 => Convert.ToUInt16(raw, CultureInfo.InvariantCulture),
            RegisterDataType.Int32 => Convert.ToInt32(raw, CultureInfo.InvariantCulture),
            RegisterDataType.UInt32 => Convert.ToUInt32(raw, CultureInfo.InvariantCulture),
            RegisterDataType.Int64 => Convert.ToInt64(raw, CultureInfo.InvariantCulture),
            RegisterDataType.UInt64 => Convert.ToUInt64(raw, CultureInfo.InvariantCulture),
            RegisterDataType.Float => Convert.ToSingle(raw, CultureInfo.InvariantCulture),
            RegisterDataType.Double => Convert.ToDouble(raw, CultureInfo.InvariantCulture),
            RegisterDataType.AsciiString => raw is byte[] bytes
                ? Encoding.ASCII.GetString(bytes).TrimEnd('\0')
                : Convert.ToString(raw, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => raw
        };
    }

    private static object? ApplyScale(object? value, float scale)
    {
        if (value is not IConvertible || value is string || scale == 1) return value;
        return Convert.ToDouble(value, CultureInfo.InvariantCulture) * (scale == 0 ? 1 : scale);
    }

    private static object Encode(object value, RegisterDataType dataType) => dataType switch
    {
        RegisterDataType.Bool => Convert.ToBoolean(value, CultureInfo.InvariantCulture),
        RegisterDataType.Byte => Convert.ToByte(value, CultureInfo.InvariantCulture),
        RegisterDataType.SByte => Convert.ToSByte(value, CultureInfo.InvariantCulture),
        RegisterDataType.Int16 => Convert.ToInt16(value, CultureInfo.InvariantCulture),
        RegisterDataType.UInt16 => Convert.ToUInt16(value, CultureInfo.InvariantCulture),
        RegisterDataType.Int32 => Convert.ToInt32(value, CultureInfo.InvariantCulture),
        RegisterDataType.UInt32 => Convert.ToUInt32(value, CultureInfo.InvariantCulture),
        RegisterDataType.Int64 => Convert.ToInt64(value, CultureInfo.InvariantCulture),
        RegisterDataType.UInt64 => Convert.ToUInt64(value, CultureInfo.InvariantCulture),
        RegisterDataType.Float => Convert.ToSingle(value, CultureInfo.InvariantCulture),
        RegisterDataType.Double => Convert.ToDouble(value, CultureInfo.InvariantCulture),
        RegisterDataType.AsciiString => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value
    };

    private static string AppendError(string? current, string error) =>
        string.IsNullOrWhiteSpace(current) ? error : $"{current}; {error}";

    private const int S7Port = 102;
}
