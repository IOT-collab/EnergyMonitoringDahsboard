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
            _dir = string.IsNullOrWhiteSpace(dataFolder) ? AppPaths.Logs : dataFolder;
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

        // readings: meterName -> configured parameter name -> value.
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

                    EnsureHeaders(ws, kv.Value?.Keys ?? Enumerable.Empty<string>());
                    var headerMap = ws.Row(1).CellsUsed()
                        .ToDictionary(c => c.GetString(), c => c.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);
                    var nextRow = ws.LastRowUsed().RowNumber() + 1;
                    ws.Cell(nextRow, headerMap["Timestamp"]).Value = DateTime.Now;
                    ws.Cell(nextRow, headerMap["Timestamp"]).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
                    foreach (var reading in kv.Value ?? new Dictionary<string, object>())
                    {
                        if (!headerMap.TryGetValue(reading.Key, out var column)) continue;
                        WriteValue(ws.Cell(nextRow, column), reading.Value);
                    }
                }
                wb.SaveAs(_filePath);
            }
        }

        private static void EnsureHeaders(IXLWorksheet ws, IEnumerable<string> parameterNames)
        {
            var existing = new HashSet<string>(ws.Row(1).CellsUsed().Select(c => c.GetString()), StringComparer.OrdinalIgnoreCase);
            var nextColumn = Math.Max(2, (ws.LastColumnUsed()?.ColumnNumber() ?? 1) + 1);
            foreach (var parameterName in parameterNames.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                if (!existing.Add(parameterName)) continue;
                ws.Cell(1, nextColumn++).Value = parameterName;
            }
            ws.Row(1).Style.Font.Bold = true;
        }

        private static void WriteValue(IXLCell cell, object value)
        {
            if (value == null) return;
            if (value is double d) cell.Value = double.IsFinite(d) ? d : 0d;
            else if (value is float f) cell.Value = float.IsFinite(f) ? f : 0f;
            else if (value is decimal dec) cell.Value = dec;
            else if (value is int i) cell.Value = i;
            else if (value is long l) cell.Value = l;
            else cell.Value = value.ToString();
        }

        private static void WriteHeader(IXLWorksheet ws)
        {
            var headers = new[] { "Timestamp" };
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
