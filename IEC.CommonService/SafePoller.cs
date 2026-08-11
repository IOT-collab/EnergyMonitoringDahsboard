using System;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IEC.CommonService
{
    public class SafePoller : IDisposable
    {
        private readonly System.Timers.Timer _timer;
        private readonly Func<Dictionary<int, object>, Task> _asyncAction; // Simplified action
        private readonly Action<Exception> _onError;
        private int _isBusy;
        private bool _disposed;

        public SafePoller(TimeSpan interval, Func<Dictionary<int,Object>,Task> asyncAction, Action<Exception> onError = null)
        {
            _asyncAction = asyncAction;
            _onError = onError;

            _timer = new System.Timers.Timer(interval.TotalMilliseconds);
            _timer.Elapsed += Timer_Elapsed; // Runs on a ThreadPool thread
        }

        private async void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (_disposed || Interlocked.Exchange(ref _isBusy, 1) == 1) return;

            try
            {
                await _asyncAction(new Dictionary<int, object>() ); // Runs your passed method
            }
            catch (Exception ex)
            {
                _onError?.Invoke(ex);
            }
            finally
            {
                Interlocked.Exchange(ref _isBusy, 0);
            }
        }

        public void Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SafePoller));

            _timer.Start();
        }

        public void Stop() => _timer.Stop();

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _timer.Stop();
            _timer.Elapsed -= Timer_Elapsed;
            _timer.Dispose();
        }
    }
}
