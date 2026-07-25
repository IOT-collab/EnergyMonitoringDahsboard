using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IEC.Shared.Models;

namespace IEC.Shared.Services
{
    // Coordinator forwards meters to the appropriate transport implementation (RTU / TCP)
    public class MultiEnergyMeterCoordinator : IMultiEnergyMeterService
    {
        private readonly MultiEnergyMeterRtuService _rtuService;
        private readonly MultiEnergyMeterTcpService _tcpService;

        public MultiEnergyMeterCoordinator(MultiEnergyMeterRtuService rtuService, MultiEnergyMeterTcpService tcpService)
        {
            _rtuService = rtuService ?? throw new ArgumentNullException(nameof(rtuService));
            _tcpService = tcpService ?? throw new ArgumentNullException(nameof(tcpService));
        }

        // Accepts mixed meters; split and forward to underlying services
        public async Task Configure(IEnumerable<MetersConfig> meters)
        {
            if (meters == null)
            {
                _rtuService.Configure(Array.Empty<MetersConfig>());
                _tcpService.Configure(Array.Empty<MetersConfig>());
                return;
            }

            var list = meters.ToList();

            var rtuMeters = list.Where(m => (m.Communication?.Protocol ?? ProtocolsType.ModbusRtu) == ProtocolsType.ModbusRtu);
            var tcpMeters = list.Where(m => (m.Communication?.Protocol ?? ProtocolsType.ModbusRtu) == ProtocolsType.ModbusTcp);

            _rtuService.Configure(rtuMeters);
            _tcpService.Configure(tcpMeters);
        }

        public async Task<Dictionary<string, MeterReading>> ReadAllAsync()
        {
            var results = new Dictionary<string, MeterReading>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var rtu = await _rtuService.ReadAllAsync().ConfigureAwait(false);
                if (rtu != null)
                {
                    foreach (var kv in rtu)
                        results[kv.Key] = kv.Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RTU ReadAllAsync error: {ex.Message}");
            }

            try
            {
                var tcp = await _tcpService.ReadAllAsync().ConfigureAwait(false);
                if (tcp != null)
                {
                    foreach (var kv in tcp)
                        results[kv.Key] = kv.Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TCP ReadAllAsync error: {ex.Message}");
            }

            return results;
        }

        public Task<MeterReading> ReadOneAsync(string meterName)
        {
            if (string.IsNullOrWhiteSpace(meterName))
                throw new ArgumentNullException(nameof(meterName));

            if (_rtuService.HasMeter(meterName))
                return _rtuService.ReadOneAsync(meterName);

            if (_tcpService.HasMeter(meterName))
                return _tcpService.ReadOneAsync(meterName);

            throw new InvalidOperationException($"Meter '{meterName}' is not configured in any transport.");
        }

        public async Task DisconnectAll()
        {
            try { await _rtuService.DisconnectAll().ConfigureAwait(false); } catch { }
            try { await _tcpService.DisconnectAll().ConfigureAwait(false); } catch { }
        }

        public void Dispose()
        {
            try { _rtuService.Dispose(); } catch { }
            try { _tcpService.Dispose(); } catch { }
        }

        // New: presence check, required by IMultiEnergyMeterService
        public bool HasMeter(string meterName)
        {
            if (string.IsNullOrWhiteSpace(meterName))
                return false;

            try
            {
                if (_rtuService.HasMeter(meterName))
                    return true;
            }
            catch { /* ignore */ }

            try
            {
                if (_tcpService.HasMeter(meterName))
                    return true;
            }
            catch { /* ignore */ }

            return false;
        }
    }
}