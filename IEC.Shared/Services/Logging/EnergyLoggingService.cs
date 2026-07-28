using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;

namespace IEC.Shared.Services.Logging
{
    // Writes per-meter readings into an Excel workbook (one worksheet per meter).
    // Requires ClosedXML (install via NuGet).
    public class EnergyLoggingService : IDisposable
    {
        private readonly string _dir;
        private string _filePath = string.Empty;
        private List<string> _meterNames = new();
        private readonly object _sync = new();

        public EnergyLoggingService(string dataFolder)
        {
            _dir = string.IsNullOrWhiteSpace(dataFolder) ? AppDomain.CurrentDomain.BaseDirectory : dataFolder;
        }

        public void Start(IEnumerable<string> meterNames, string fileName = null)
        {
            _meterNames = meterNames?.ToList() ?? new List<string> { "Meter1" };
            Directory.CreateDirectory(_dir);
            fileName ??= $"Energy_{DateTime.Now:yyyyMMdd}.xlsx";
            if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                fileName += ".xlsx";
            _filePath = Path.Combine(_dir, fileName);

            lock (_sync)
            {
                if (!File.Exists(_filePath))
                {
                    using var wb = new XLWorkbook();
                    foreach (var m in _meterNames)
                    {
                        var name = SanitizeSheetName(m);
                        var ws = wb.AddWorksheet(name);
                        WriteHeader(ws);
                    }
                    wb.SaveAs(_filePath);
                }
                else
                {
                    // Ensure worksheets and header exist for configured meters
                    using var wb = new XLWorkbook(_filePath);
                    var changed = false;
                    foreach (var m in _meterNames)
                    {
                        var name = SanitizeSheetName(m);
                        if (!wb.Worksheets.Any(w => w.Name == name))
                        {
                            var ws = wb.AddWorksheet(name);
                            WriteHeader(ws);
                            changed = true;
                        }
                    }
                    if (changed) wb.SaveAs(_filePath);
                }
            }
        }

        public void Stop()
        {
            _filePath = string.Empty;
            _meterNames.Clear();
        }

        // readings: meterName -> (key -> value). Expected keys: VoltageA,VoltageB,VoltageC,CurrentA,CurrentB,CurrentC,ActivePower,ReactivePower,ApparentPower,Frequency,PowerFactor
        public void AppendReadings(IDictionary<string, IDictionary<string, object>> readings)
        {
            if (string.IsNullOrEmpty(_filePath) || readings == null || !readings.Any()) return;
            lock (_sync)
            {
                using var wb = File.Exists(_filePath) ? new XLWorkbook(_filePath) : new XLWorkbook();
                foreach (var kv in readings)
                {
                    var meterName = SanitizeSheetName(kv.Key ?? "Meter");
                    var ws = wb.Worksheets.FirstOrDefault(w => w.Name == meterName) ?? wb.AddWorksheet(meterName);

                    if (ws.LastRowUsed() == null)
                        WriteHeader(ws);

                    var nextRow = ws.LastRowUsed().RowNumber() + 1;
                    ws.Cell(nextRow, 1).Value = DateTime.Now.ToLongTimeString() ;
                    ws.Cell(nextRow, 2).Value = Safe(kv.Value, "VoltageA");
                    ws.Cell(nextRow, 3).Value = Safe(kv.Value, "VoltageB");
                    ws.Cell(nextRow, 4).Value = Safe(kv.Value, "VoltageC");
                    ws.Cell(nextRow, 5).Value = Safe(kv.Value, "CurrentA");
                    ws.Cell(nextRow, 6).Value = Safe(kv.Value, "CurrentB");
                    ws.Cell(nextRow, 7).Value = Safe(kv.Value, "CurrentC");
                    ws.Cell(nextRow, 8).Value = Safe(kv.Value, "ActivePower");
                    ws.Cell(nextRow, 9).Value = Safe(kv.Value, "ReactivePower");
                    ws.Cell(nextRow, 10).Value = Safe(kv.Value, "ApparentPower");
                    ws.Cell(nextRow, 11).Value = Safe(kv.Value, "Frequency");
                    ws.Cell(nextRow, 12).Value = Safe(kv.Value, "PowerFactor");
                }
                wb.SaveAs(_filePath);
            }
        }

        private static string Safe(IDictionary<string, object> d, string k)
            => d != null && d.TryGetValue(k, out var v) ? v?.ToString() ?? string.Empty : string.Empty;

        private static void WriteHeader(IXLWorksheet ws)
        {
            var headers = new[]
            {
                "Timestamp","VoltageA","VoltageB","VoltageC",
                "CurrentA","CurrentB","CurrentC",
                "ActivePower","ReactivePower","ApparentPower",
                "Frequency","PowerFactor"
            };
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];
            ws.Row(1).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();
        }

        // Excel sheet names limited to 31 chars and cannot contain some chars.
        private static string SanitizeSheetName(string name)
        {
            var invalid = new[] { '\\', '/', '?', '*', '[', ']' };
            var s = new string(name.Where(c => !invalid.Contains(c)).ToArray());
            if (s.Length == 0) s = "Meter";
            return s.Length > 31 ? s.Substring(0, 31) : s;
        }

        public void Dispose() => Stop();
    }
}