using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IECGUI.Services
{
    // Lightweight service: start/stop logging and append one CSV row containing readings for all meters.
    public class EnergyLoggingService : IDisposable
    {
        private readonly string _dir;
        private string _filePath = string.Empty;
        private List<string> _meterNames = new();

        public EnergyLoggingService(string dataFolder)
        {
            _dir = dataFolder ?? AppDomain.CurrentDomain.BaseDirectory;
        }

        public void Start(IEnumerable<string> meterNames, string fileName = null)
        {
            _meterNames = meterNames?.ToList() ?? new List<string> { "Meter1" };
            Directory.CreateDirectory(_dir);
            _filePath = Path.Combine(_dir, fileName ?? $"Energy_{DateTime.Now:yyyyMMdd}.csv");
            if (!File.Exists(_filePath) || new FileInfo(_filePath).Length == 0)
            {
                File.WriteAllText(_filePath, BuildHeaderLine() + Environment.NewLine, Encoding.UTF8);
            }
        }

        public void Stop()
        {
            // Nothing to dispose for plain file append; placeholder for future flush/cleanup.
            _filePath = string.Empty;
            _meterNames.Clear();
        }

        // readings: key = meter name, value = dictionary of named values for that meter (VoltageA, CurrentA, PowerKW, Frequency...)
        public void AppendReadings(IDictionary<string, IDictionary<string, object>> readings)
        {
            if (string.IsNullOrEmpty(_filePath) || readings == null) return;

            var line = new StringBuilder();
            line.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            foreach (var name in _meterNames)
            {
                if (readings.TryGetValue(name, out var values) && values != null)
                {
                    // ensure stable column order: VoltageA,VoltageB,VoltageC,CurrentA,CurrentB,CurrentC,ActivePower,ReactivePower,ApparentPower,Frequency,PowerFactor
                    line.Append($",{Safe(values, "VoltageA")},{Safe(values, "VoltageB")},{Safe(values, "VoltageC")}");
                    line.Append($",{Safe(values, "CurrentA")},{Safe(values, "CurrentB")},{Safe(values, "CurrentC")}");
                    line.Append($",{Safe(values, "ActivePower")},{Safe(values, "ReactivePower")},{Safe(values, "ApparentPower")}");
                    line.Append($",{Safe(values, "Frequency")},{Safe(values, "PowerFactor")}");
                }
                else
                {
                    // pad empty fields for this meter
                    line.Append(", , , , , , , , , , ");
                }
            }

            File.AppendAllText(_filePath, line.ToString() + Environment.NewLine, Encoding.UTF8);
        }

        private static string Safe(IDictionary<string, object> d, string k)
            => d != null && d.TryGetValue(k, out var v) ? v?.ToString() ?? string.Empty : string.Empty;

        private string BuildHeaderLine()
        {
            var sb = new StringBuilder();
            sb.Append("Timestamp");
            foreach (var name in _meterNames)
            {
                sb.Append($",[{name}]VoltageA,[{name}]VoltageB,[{name}]VoltageC");
                sb.Append($",[{name}]CurrentA,[{name}]CurrentB,[{name}]CurrentC");
                sb.Append($",[{name}]ActivePower,[{name}]ReactivePower,[{name}]ApparentPower");
                sb.Append($",[{name}]Frequency,[{name}]PowerFactor");
            }
            return sb.ToString();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}