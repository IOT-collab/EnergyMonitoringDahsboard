using IEC.Shared.IECInterface;
using IEC.Shared.IECModels;
using IEC61850.Client;
using IEC61850.Common;

namespace IEC.Shared.IECServices
{
    public class Iec61850MeterService : IIec61850MeterService
    {
        // Ek relay = ek connection
        private class RelayConnection
        {
            public RelayConfig Config { get; set; }
            public IedConnection Connection { get; set; }
            public bool IsConnected { get; set; }
            public string LastError { get; set; }
            public readonly object Lock = new object();
        }

        // RelayId → Connection
        private readonly Dictionary<int, RelayConnection> _connections = new();

        public bool IsConnected => _connections.Values.Any(c => c.IsConnected);

        // ── Single Relay (purana method — backward compatible) ──
        public void Connect(string hostname, int port = 102)
        {
            var singleRelay = new RelayConfig
            {
                RelayId = 1,
                RelayName = "Default Relay",
                IPAddress = hostname,
                Port = port,
                IsEnabled = true,
                LogicalNodes = new List<LnConfig>
                {
                    new LnConfig
                    {
                        LogicalDevice = "IED_1234MEAS",
                        LogicalNode   = "MMXU1",
                        FC            = "MX"
                    }
                }
            };

            ConnectSingle(singleRelay);
        }

        // ── Multi Relay Connect ─────────────────────────────────
        public async Task ConnectAllAsync(List<RelayConfig> relays)
        {
            var tasks = relays
                .Where(r => r.IsEnabled)
                .Select(r => Task.Run(() => ConnectSingle(r)));

            await Task.WhenAll(tasks);
        }

        private void ConnectSingle(RelayConfig config)
        {
            try
            {
                var con = new IedConnection();
                con.Connect(config.IPAddress, config.Port);

                _connections[config.RelayId] = new RelayConnection
                {
                    Config = config,
                    Connection = con,
                    IsConnected = true
                };

                Console.WriteLine($"[IEC61850] Connected: {config.RelayName} ({config.IPAddress})");
            }
            catch (Exception ex)
            {
                _connections[config.RelayId] = new RelayConnection
                {
                    Config = config,
                    IsConnected = false,
                    LastError = ex.Message
                };

                Console.WriteLine($"[IEC61850] Failed: {config.RelayName} — {ex.Message}");
            }
        }

        // ── Read All Relays ─────────────────────────────────────
        public async Task<Dictionary<int, RelayReadingModel>> ReadAllAsync()
        {
            try
            {
                var tasks = _connections.Values
                    .Select(async conn =>
                    {
                        var reading = await ReadFromConnection(conn);
                        return (conn.Config.RelayId, reading);
                    });

                var results = await Task.WhenAll(tasks);

                return results.ToDictionary(r => r.RelayId, r => r.reading);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[READ ALL ERROR] {ex.Message}");
                return new Dictionary<int, RelayReadingModel>();
            }
        }

        // ── Read Single Relay by ID ─────────────────────────────
        public Task<RelayReadingModel> ReadOneAsync(int relayId)
        {
            if (!_connections.TryGetValue(relayId, out var conn))
                return Task.FromResult(new RelayReadingModel
                {
                    IsOnline = false,
                    ErrorMessage = $"Relay ID {relayId} not found"
                });

            return ReadFromConnection(conn);
        }

        // ── Core Read Logic — Config Driven ────────────────────
        private Task<RelayReadingModel> ReadFromConnection(RelayConnection conn)
        {
            return Task.Run(() =>
            {
                if (conn == null)
                    return new RelayReadingModel
                    {
                        IsOnline = false,
                        ErrorMessage = "Connection is null"
                    };
                lock (conn.Lock)
                {
                    if (!conn.IsConnected)
                        return new RelayReadingModel
                        {
                            IsOnline = false,
                            ErrorMessage = conn.LastError
                        };

                    try
                    {
                        var reading = new RelayReadingModel
                        {
                            IsOnline = true,
                            RelayId = conn.Config.RelayId,
                            RelayName = conn.Config.RelayName
                        };

                        // Har configured LN ke liye read karo
                        foreach (var lnConfig in conn.Config.LogicalNodes)
                        {
                            string path = $"{lnConfig.LogicalDevice}/{lnConfig.LogicalNode}";
                            FunctionalConstraint fc = ParseFC(lnConfig.FC);

                            MmsValue lnValue = conn.Connection.ReadValue(path, fc);

                            // Har configured DO mapping ke liye value extract karo
                            foreach (var mapping in lnConfig.Mappings.Where(m => m.IsEnabled))
                            {
                                try
                                {
                                    float value = ExtractValue(lnValue, mapping);
                                    reading.Values[mapping.ParameterName] = value;
                                }
                                catch (Exception ex)
                                {
                                    reading.Values[mapping.ParameterName] = float.NaN;
                                    Console.WriteLine(
                                        $"[READ ERROR] {mapping.ParameterName}: {ex.Message}");
                                }
                            }
                        }

                        return reading;
                    }
                    catch (Exception ex)
                    {
                        conn.IsConnected = false;
                        conn.LastError = ex.Message;

                        return new RelayReadingModel
                        {
                            IsOnline = false,
                            RelayId = conn.Config.RelayId,
                            RelayName = conn.Config.RelayName,
                            ErrorMessage = ex.Message
                        };
                    }
                }
            });
        }

        // ── Value Extraction — Config se ───────────────────────
        private float ExtractValue(MmsValue lnValue, DoMappingConfig mapping)
        {
            MmsValue doVal = lnValue.GetElement(mapping.DOIndex);

            if (mapping.ValueType == IECModels.ValueType.Direct)
            {
                // eg: Hz, TotW, TotPF
                return GetDirectValue(doVal);
            }
            else
            {
                // eg: PhV.phsA, PPV.phsAB, A.phsA
                MmsValue phaseChild = doVal.GetElement(mapping.PhaseIndex);
                return GetPhaseValue(phaseChild);
            }
        }

        private float GetDirectValue(MmsValue doVal)
        {
            return (float)doVal.GetElement(1)
                               .GetElement(0)
                               .ToDouble();
        }

        private float GetPhaseValue(MmsValue phaseChild)
        {
            return (float)phaseChild.GetElement(1)
                                    .GetElement(0)
                                    .GetElement(0)
                                    .ToDouble();
        }

        private FunctionalConstraint ParseFC(string fc) => fc switch
        {
            "MX" => FunctionalConstraint.MX,
            "ST" => FunctionalConstraint.ST,
            "CF" => FunctionalConstraint.CF,
            "DC" => FunctionalConstraint.DC,
            _ => FunctionalConstraint.MX
        };

        // ── Disconnect ──────────────────────────────────────────
        public void Disconnect()
        {
            foreach (var conn in _connections.Values)
            {
                try { conn.Connection?.Release(); } catch { }
                conn.IsConnected = false;
            }
            _connections.Clear();
        }

        public void Dispose() => Disconnect();

        // ── Backward compatible single ReadAsync ────────────────
        public Task<RelayReadingModel> ReadAsync()
            => ReadOneAsync(1);
    }
}