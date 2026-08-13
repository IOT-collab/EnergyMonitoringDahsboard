using IEC.CommonService;
using IEC.Shared.Models;
using IEC.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IECGUI.Services
{
    // The single application-level owner of Modbus configuration, connectivity and polling.
    public sealed class DeviceRuntimeService : ObservableObjectVM, IDisposable
    {
        private readonly IMultiEnergyMeterService _devices;
        private readonly ConfigurationManagerService _configuration;
        private readonly SafePoller _poller;
        private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
        private readonly SemaphoreSlim _pollLock = new(1, 1);
        private readonly object _snapshotLock = new();
        private Dictionary<string, MeterReading> _snapshot = new(StringComparer.OrdinalIgnoreCase);
        private bool _isRunning;
        private string _status = "Disconnected";
        private DateTime? _lastSuccessfulPoll;

        public bool IsRunning { get => _isRunning; private set => SetProperty(ref _isRunning, value); }
        public string Status { get => _status; private set => SetProperty(ref _status, value); }
        public DateTime? LastSuccessfulPoll { get => _lastSuccessfulPoll; private set => SetProperty(ref _lastSuccessfulPoll, value); }

        public DeviceRuntimeService(IMultiEnergyMeterService devices, ConfigurationManagerService configuration)
        {
            _devices = devices;
            _configuration = configuration;
            _poller = new SafePoller(TimeSpan.FromMilliseconds(500), _ => PollAsync(),
                ex => Status = $"Polling error: {ex.Message}");
        }

        public async Task StartAsync() => await ReconfigureAsync(false).ConfigureAwait(false);

        public async Task ReconfigureAsync(bool force = true)
        {
            await _lifecycleLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (IsRunning && !force) return;
                Status = "Connecting...";
                _poller.Stop();
                IsRunning = false;
                await _pollLock.WaitAsync().ConfigureAwait(false);
                _pollLock.Release();
                var enabled = (_configuration.Configuration.Meters ?? new List<MetersConfig>())
                    .Where(x => x != null && x.IsEnabled && !string.IsNullOrWhiteSpace(x.MeterName))
                    .ToList();
                await _devices.Configure(enabled).ConfigureAwait(false);
                lock (_snapshotLock) _snapshot.Clear();
                IsRunning = true;
                Status = enabled.Count == 0 ? "No enabled devices" : $"Connected ({enabled.Count} configured)";
                _poller.Start();
                await PollAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                IsRunning = false;
                Status = $"Connection error: {ex.Message}";
                throw;
            }
            finally { _lifecycleLock.Release(); }
        }

        public async Task StopAsync()
        {
            await _lifecycleLock.WaitAsync().ConfigureAwait(false);
            try
            {
                _poller.Stop();
                IsRunning = false;
                await _pollLock.WaitAsync().ConfigureAwait(false);
                _pollLock.Release();
                await _devices.DisconnectAll().ConfigureAwait(false);
                lock (_snapshotLock) _snapshot.Clear();
                Status = "Disconnected";
            }
            finally { _lifecycleLock.Release(); }
        }

        public Dictionary<string, MeterReading> GetSnapshot()
        {
            lock (_snapshotLock)
                return new Dictionary<string, MeterReading>(_snapshot, StringComparer.OrdinalIgnoreCase);
        }

        private async Task PollAsync()
        {
            await _pollLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!IsRunning) return;
                var readings = await _devices.ReadAllAsync().ConfigureAwait(false);
                lock (_snapshotLock)
                    _snapshot = new Dictionary<string, MeterReading>(readings, StringComparer.OrdinalIgnoreCase);
                LastSuccessfulPoll = DateTime.Now;
                var online = readings.Values.Count(x => x != null && x.Values.Any(v => v.Value != null));
                Status = $"Running - {online}/{readings.Count} responding";
            }
            finally { _pollLock.Release(); }
        }

        public void Dispose()
        {
            _poller.Dispose();
            _lifecycleLock.Dispose();
            _pollLock.Dispose();
        }
    }
}
