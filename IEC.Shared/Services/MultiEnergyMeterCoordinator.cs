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
        private readonly IOpcMeterService _opcUaService;
        private readonly IOpcMeterService _opcDaService;

        private readonly McSlmpDeviceService _mcService;
        public MultiEnergyMeterCoordinator(
            MultiEnergyMeterRtuService rtuService,
            MultiEnergyMeterTcpService tcpService,
            IOpcMeterService? opcUaService = null,
            IOpcMeterService? opcDaService = null,
            McSlmpDeviceService? mcService = null)
        {
            _rtuService = rtuService ?? throw new ArgumentNullException(nameof(rtuService));
            _tcpService = tcpService ?? throw new ArgumentNullException(nameof(tcpService));
            _opcUaService = opcUaService ?? new OpcUaDeviceService();
            _opcDaService = opcDaService ?? new OpcDaDeviceService();
            _mcService = mcService ?? new McSlmpDeviceService();
        }

        // Accepts mixed meters; split and forward to underlying services
        public async Task Configure(IEnumerable<MetersConfig> meters)
        {
            if (meters == null)
            {
                await _rtuService.Configure(Array.Empty<MetersConfig>()).ConfigureAwait(false);
                await _tcpService.Configure(Array.Empty<MetersConfig>()).ConfigureAwait(false);
                await _opcUaService.Configure(Array.Empty<MetersConfig>()).ConfigureAwait(false);
                await _opcDaService.Configure(Array.Empty<MetersConfig>()).ConfigureAwait(false);
                await _mcService.Configure(Array.Empty<MetersConfig>()).ConfigureAwait(false);
                return;
            }

            var list = meters.ToList();

            var rtuMeters = list.Where(m => (m.Communication?.Protocol ?? ProtocolsType.ModbusRtu) == ProtocolsType.ModbusRtu);
            var tcpMeters = list.Where(m => (m.Communication?.Protocol ?? ProtocolsType.ModbusRtu) == ProtocolsType.ModbusTcp);
            var uaMeters = list.Where(m => m.Communication?.Protocol == ProtocolsType.OpcUa);
            var daMeters = list.Where(m => m.Communication?.Protocol == ProtocolsType.OpcDa);

            var mcMeters = list.Where(m => m.Communication?.Protocol == ProtocolsType.McSlmp);
            // Configuration must complete before the caller starts polling.
            // Previously these tasks were fire-and-forget, so ReadAllAsync could
            // run while the RTU service had just cleared its meter dictionaries.
            await _rtuService.Configure(rtuMeters).ConfigureAwait(false);
            await _tcpService.Configure(tcpMeters).ConfigureAwait(false);
            await _opcUaService.Configure(uaMeters).ConfigureAwait(false);
            await _opcDaService.Configure(daMeters).ConfigureAwait(false);
            await _mcService.Configure(mcMeters).ConfigureAwait(false);
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

            try
            {
                var ua = await _opcUaService.ReadAllAsync().ConfigureAwait(false);
                if (ua != null)
                    foreach (var kv in ua) results[kv.Key] = kv.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OPC UA ReadAllAsync error: {ex.Message}");
            }

            try
            {
                var da = await _opcDaService.ReadAllAsync().ConfigureAwait(false);
                if (da != null)
                    foreach (var kv in da) results[kv.Key] = kv.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OPC DA ReadAllAsync error: {ex.Message}");

            }

            try
            {
                var mc = await _mcService.ReadAllAsync().ConfigureAwait(false);
                if (mc != null)
                    foreach (var kv in mc) results[kv.Key] = kv.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MC/SLMP ReadAllAsync error: {ex.Message}");
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

            if (_opcUaService.HasMeter(meterName))
                return _opcUaService.ReadOneAsync(meterName);

            if (_opcDaService.HasMeter(meterName))
                return _opcDaService.ReadOneAsync(meterName);


            if (_mcService.HasMeter(meterName))
                return _mcService.ReadOneAsync(meterName);
            throw new InvalidOperationException($"Meter '{meterName}' is not configured in any transport.");
        }

        public async Task DisconnectAll()
        {
            try { await _rtuService.DisconnectAll().ConfigureAwait(false); } catch { }
            try { await _tcpService.DisconnectAll().ConfigureAwait(false); } catch { }
            try { await _opcUaService.DisconnectAll().ConfigureAwait(false); } catch { }
            try { await _opcDaService.DisconnectAll().ConfigureAwait(false); } catch { }
            try { await _mcService.DisconnectAll().ConfigureAwait(false); } catch { }
        }

        public void Dispose()
        {
            try { _rtuService.Dispose(); } catch { }
            try { _tcpService.Dispose(); } catch { }
            try { _opcUaService.Dispose(); } catch { }
            try { _opcDaService.Dispose(); } catch { }
            try { _mcService.Dispose(); } catch { }
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

            try
            {
                if (_opcUaService.HasMeter(meterName) || _opcDaService.HasMeter(meterName))
                    return true;
                if (_mcService.HasMeter(meterName))
                    return true;
            }
            catch { /* ignore */ }

            return false;
        }

        public Task WriteCoilAsync(string meterName, ushort address, bool value) =>
            _rtuService.HasMeter(meterName) ? _rtuService.WriteCoilAsync(meterName, address, value) :
            _tcpService.HasMeter(meterName) ? _tcpService.WriteCoilAsync(meterName, address, value) :
            throw new InvalidOperationException($"Device '{meterName}' is not configured for a Modbus write.");

        public Task WriteRegisterAsync(string meterName, ushort address, ushort value) =>
            _rtuService.HasMeter(meterName) ? _rtuService.WriteRegisterAsync(meterName, address, value) :
            _tcpService.HasMeter(meterName) ? _tcpService.WriteRegisterAsync(meterName, address, value) :
            throw new InvalidOperationException($"Device '{meterName}' is not configured for a Modbus write.");

        public Task<bool> ReadBooleanAsync(string meterName, ModbusDataArea area, ushort address) =>
            _rtuService.HasMeter(meterName) ? _rtuService.ReadBooleanAsync(meterName, area, address) :
            _tcpService.HasMeter(meterName) ? _tcpService.ReadBooleanAsync(meterName, area, address) :
            throw new InvalidOperationException($"Device '{meterName}' is not configured for a Modbus read.");
        public Task WriteMappedValueAsync(string meterName, string parameterName, object value)
        {
            if (_mcService.HasMeter(meterName))
                return _mcService.WriteAsync(meterName, parameterName, value);
            throw new InvalidOperationException($"Device '{meterName}' is not configured for an MC/SLMP mapped write.");
        }
    }
}
