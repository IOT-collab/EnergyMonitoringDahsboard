using IEC.Shared.Models;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Text;

namespace IEC.Shared.Services;

/// <summary>
/// Common boundary for optional OPC transports. OPC client implementations are
/// deliberately kept out of the Modbus project so the application can still be
/// installed on machines that do not have an OPC runtime/server.
/// </summary>
public interface IOpcMeterService : IDisposable
{
    Task Configure(IEnumerable<MetersConfig> meters);
    Task<Dictionary<string, MeterReading>> ReadAllAsync();
    Task<MeterReading> ReadOneAsync(string meterName);
    Task DisconnectAll();
    bool HasMeter(string meterName);
}

/// <summary>OPC UA client using the OPC Foundation UA .NET Standard stack.</summary>
public sealed class OpcUaDeviceService : IOpcMeterService
{
    private readonly Dictionary<string, MetersConfig> _meters = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private ISession? _session;
    private string? _endpointUrl;

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
        {
            try { result[meter.MeterName] = await ReadMeterAsync(meter).ConfigureAwait(false); }
            catch (Exception ex)
            {
                result[meter.MeterName] = ErrorReading(meter, ex.Message);
            }
        }
        return result;
    }

    public Task<MeterReading> ReadOneAsync(string meterName)
    {
        if (!_meters.TryGetValue(meterName ?? string.Empty, out var meter))
            throw new InvalidOperationException($"OPC UA meter '{meterName}' is not configured.");
        return ReadMeterAsync(meter);
    }

    public async Task DisconnectAll()
    {
        await _sessionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_session != null)
            {
                try { _session.Close(); } catch { }
                _session.Dispose();
                _session = null;
            }
            _endpointUrl = null;
        }
        finally { _sessionLock.Release(); }
    }

    public bool HasMeter(string meterName) => !string.IsNullOrWhiteSpace(meterName) && _meters.ContainsKey(meterName);

    public void Dispose()
    {
        try { DisconnectAll().GetAwaiter().GetResult(); } catch { }
        _sessionLock.Dispose();
        _meters.Clear();
    }

    private async Task<MeterReading> ReadMeterAsync(MetersConfig meter)
    {
        var communication = meter.Communication;
        var endpoint = communication?.OpcEndpointUrl?.Trim();
        if (string.IsNullOrWhiteSpace(endpoint))
            return ErrorReading(meter, "OPC UA endpoint is not configured. Enter an opc.tcp:// URL.");

        await EnsureSessionAsync(communication!, endpoint).ConfigureAwait(false);
        var reading = new MeterReading { MeterName = meter.MeterName, Timestamp = DateTime.UtcNow };
        foreach (var register in meter.Registers?.Where(r => r.IsEnabled) ?? Enumerable.Empty<RegisterConfig>())
        {
            var address = register.Address?.Trim();
            if (string.IsNullOrWhiteSpace(address)) continue;
            try
            {
                var value = await _session!.ReadValueAsync(NodeId.Parse(address)).ConfigureAwait(false);
                if (StatusCode.IsGood(value.StatusCode))
                    reading.Values[string.IsNullOrWhiteSpace(register.ParameterName) ? address : register.ParameterName] = value.Value;
                else
                    reading.CommunicationError = $"NodeId '{address}' returned {value.StatusCode}.";
            }
            catch (Exception ex)
            {
                reading.CommunicationError = $"NodeId '{address}' read failed: {ex.Message}";
            }
        }
        if (reading.Values.Count == 0 && string.IsNullOrWhiteSpace(reading.CommunicationError))
            reading.CommunicationError = "No enabled OPC UA NodeId mappings are configured.";
        return reading;
    }

    private async Task EnsureSessionAsync(CommunicationConfig communication, string endpointUrl)
    {
        if (_session?.Connected == true && string.Equals(_endpointUrl, endpointUrl, StringComparison.OrdinalIgnoreCase))
            return;

        await _sessionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_session?.Connected == true && string.Equals(_endpointUrl, endpointUrl, StringComparison.OrdinalIgnoreCase))
                return;

            if (_session != null) { try { _session.Close(); } catch { } _session.Dispose(); _session = null; }

            var application = new ApplicationInstance((ITelemetryContext?)null)
            {
                ApplicationName = "VEMT Energy Monitoring",
                ApplicationType = ApplicationType.Client
            };
            // Build the ApplicationConfiguration before selecting the client
            // profile.  Calling AsClient() directly on a newly constructed
            // ApplicationConfigurationBuilder leaves the application config
            // uninitialized in recent OPC Foundation package versions.
            var configuration = await application
                .Build("urn:localhost:VEMT-EnergyMonitoring", "urn:vertexgroups:VEMT")
                .AsClient()
                .AddSecurityConfiguration("CN=VEMT Energy Monitoring")
                .SetAutoAcceptUntrustedCertificates(!communication.OpcUseSecurity)
                .CreateAsync()
                .ConfigureAwait(false);

            var endpoint = CoreClientUtils.SelectEndpoint(configuration, endpointUrl, communication.OpcUseSecurity);
            var configuredEndpoint = new ConfiguredEndpoint(null, endpoint);
            IUserIdentity identity = string.IsNullOrWhiteSpace(communication.OpcUsername)
                ? new UserIdentity(new AnonymousIdentityToken())
                : new UserIdentity(communication.OpcUsername,
                    Encoding.UTF8.GetBytes(communication.OpcPassword ?? string.Empty));

            _session = await Session.Create(configuration, configuredEndpoint, false,
                "VEMT", 60000, identity, null, CancellationToken.None).ConfigureAwait(false);
            _endpointUrl = endpointUrl;
        }
        finally { _sessionLock.Release(); }
    }

    private static MeterReading ErrorReading(MetersConfig meter, string error) => new()
    {
        MeterName = meter.MeterName,
        Timestamp = DateTime.UtcNow,
        CommunicationError = error
    };
}

/// <summary>
/// OPC DA transport seam. OPC DA is COM/DCOM based and requires a vendor OPC
/// Automation/interop SDK and a registered DA server. This fallback preserves
/// the configured meter and returns a clear status until that SDK is selected.
/// </summary>
public sealed class OpcDaDeviceService : IOpcMeterService
{
    private readonly Dictionary<string, MetersConfig> _meters = new(StringComparer.OrdinalIgnoreCase);

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

    public Task<Dictionary<string, MeterReading>> ReadAllAsync() =>
        Task.FromResult(_meters.Values.ToDictionary(m => m.MeterName, CreateReading, StringComparer.OrdinalIgnoreCase));

    public Task<MeterReading> ReadOneAsync(string meterName)
    {
        if (!_meters.TryGetValue(meterName ?? string.Empty, out var meter))
            throw new InvalidOperationException($"OPC DA meter '{meterName}' is not configured.");
        return Task.FromResult(CreateReading(meter));
    }

    public Task DisconnectAll() => Task.CompletedTask;

    public bool HasMeter(string meterName) => !string.IsNullOrWhiteSpace(meterName) && _meters.ContainsKey(meterName);

    public void Dispose() => _meters.Clear();

    private static MeterReading CreateReading(MetersConfig meter)
    {
        var server = meter.Communication?.OpcServerName;
        var configuredAddress = meter.Registers?.Count(r => r.IsEnabled && !string.IsNullOrWhiteSpace(r.Address)) ?? 0;
        return new MeterReading
        {
            MeterName = meter.MeterName,
            Timestamp = DateTime.UtcNow,
            CommunicationError = string.IsNullOrWhiteSpace(server)
                ? "OPC DA server ProgID is not configured."
                : $"OPC DA client/COM server is not available (ProgID configured; {configuredAddress} ItemId mapping(s))."
        };
    }
}
