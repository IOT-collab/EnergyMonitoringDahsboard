using IEC.Shared;
using IEC.Shared.Models;
using IECGUI.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ClosedXML.Excel; 


namespace IECGUI.ViewModel
{
    public class ReportViewerViewModel : BaseViewModel
    {
        public ObservableCollection<ReportFormatConfig> ReportFormats { get; set; } = new();

        private readonly INavigationService _navigation;

        private readonly Dictionary<string, DataTable> _perMeterTables = new();

        // Optionally expose perMeterTables for export usage
        public IReadOnlyDictionary<string, DataTable> PerMeterTables => _perMeterTables;

        private ReportFormatConfig _selectedReportFormat;
        public ReportFormatConfig SelectedReportFormat
        {
            get => _selectedReportFormat;
            set
            {
                _selectedReportFormat = value;
                OnPropertyChanged(nameof(SelectedReportFormat));
                OnPropertyChanged(nameof(SelectedFormatColumns));
            }
        }

        public IEnumerable<string> SelectedFormatColumns => SelectedReportFormat?.SelectedColumns ?? Enumerable.Empty<string>();

        private DateTime _dateFrom = DateTime.Today.AddDays(-7  );
        public DateTime DateFrom
        {
            get => _dateFrom;
            set => SetProperty(ref _dateFrom, value);
     
        }

        private DateTime _dateTo = DateTime.Today;
        public DateTime DateTo
        {
            get => _dateTo;
            set => SetProperty(ref _dateTo, value);
        }

        private int _totalRowsLoaded;
        public int TotalRowsLoaded
        {
            get => _totalRowsLoaded;
            set => SetProperty(ref _totalRowsLoaded, value);
        }
        public ICommand MenuCommand { get; }
        public ICommand LoadDataCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand RefreshFormatsCommand { get; }

        public ICommand ConfigViewCommand { get; }
        public DataTable ReportDataTable { get; set; } = new DataTable();

        private readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ReportFormats.json");
        private string _prodCsvFolder;

        public ReportViewerViewModel(INavigationService navigation)
        {
            LoadReportFormats();
            LoadDataCommand = new RelayCommand(LoadData);
            ExportCsvCommand = new RelayCommand(ExportToCsv);
            RefreshFormatsCommand = new RelayCommand(RefreshFormats);
            ConfigViewCommand = new RelayCommand(() => _navigation.NavigateTo<ReportConfigViewModel>());
            MenuCommand = new RelayCommand(() => _navigation.NavigateTo<HomePageViewModel>());
            _navigation = navigation;
            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            _prodCsvFolder = Path.Combine(appFolder, "Data");
        }

        private void RefreshFormats()
        {
            var currentSelection = SelectedReportFormat?.Name;
            LoadReportFormats();
            // Restore selection if the format still exists
            if (!string.IsNullOrEmpty(currentSelection))
            {
                SelectedReportFormat = ReportFormats.FirstOrDefault(f => f.Name == currentSelection);
            }
            OnPropertyChanged(nameof(ReportFormats));
        }

        private void LoadReportFormats()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var list = JsonSerializer.Deserialize<List<ReportFormatConfig>>(json);
                    if (list != null)
                    {
                        ReportFormats.Clear();
                        foreach (var f in list) ReportFormats.Add(f);
                    }
                }
            }
            catch { }
        }

        private void LoadData()
        {
            // Refresh formats before loading to get latest
            RefreshFormats();

            ReportDataTable = new DataTable();
            _perMeterTables.Clear();
            TotalRowsLoaded = 0;

            if (SelectedReportFormat == null || SelectedReportFormat.SelectedColumns == null || !SelectedReportFormat.SelectedColumns.Any())
            {
                MessageBox.Show("No report format or columns selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                OnPropertyChanged(nameof(ReportDataTable));
                return;
            }

            // Prepare main DataTable using selected columns
            foreach (var col in SelectedReportFormat.SelectedColumns)
                ReportDataTable.Columns.Add(col);

            // iterate date range and load either CSV or Excel files
            for (var date = DateFrom.Date; date <= DateTo.Date; date = date.AddDays(1))
            {
                // 1) CSV (legacy) file
                var csvPath = Path.Combine(_prodCsvFolder, $"Production_{date:yyyyMMdd}.csv");
                if (File.Exists(csvPath))
                {
                    AppendRowsFromCsv(csvPath, ReportDataTable, SelectedReportFormat.SelectedColumns);
                    continue;
                }

                // 2) Excel files produced by logger: try pattern "Energy_{date}*.xlsx"
                var files = Directory.GetFiles(_prodCsvFolder, $"Energy_{date:yyyyMMdd}*.xlsx");
                foreach (var file in files)
                {
                    try
                    {
                        using var wb = new XLWorkbook(file);
                        foreach (var ws in wb.Worksheets)
                        {
                            var sheetName = ws.Name;
                            // ensure per-meter table exists
                            if (!_perMeterTables.TryGetValue(sheetName, out var dt))
                            {
                                dt = new DataTable(sheetName);
                                foreach (var col in SelectedReportFormat.SelectedColumns)
                                    dt.Columns.Add(col);
                                _perMeterTables[sheetName] = dt;
                            }

                            // determine header row (assume first used row)
                            var firstRow = ws.FirstRowUsed();
                            if (firstRow == null) continue;
                            var headerCells = firstRow.CellsUsed().Select(c => NormalizeHeader(c.GetString().Trim())).ToArray();

                            var lastRow = ws.LastRowUsed();
                            if (lastRow == null || lastRow.RowNumber() <= firstRow.RowNumber()) continue;

                            for (int r = firstRow.RowNumber() + 1; r <= lastRow.RowNumber(); r++)
                            {
                                var row = ws.Row(r);
                                var dataRow = ReportDataTable.NewRow();
                                var perMeterRow = dt.NewRow();

                                for (int ci = 0; ci < SelectedReportFormat.SelectedColumns.Count; ci++)
                                {
                                    var colName = SelectedReportFormat.SelectedColumns.ElementAt(ci);
                                    // try case-insensitive header match
                                    int hdrIndex = Array.FindIndex(headerCells, h => string.Equals(h, colName, StringComparison.OrdinalIgnoreCase)
                                                                                     || string.Equals(h.Replace("_",""), colName.Replace("_",""), StringComparison.OrdinalIgnoreCase)
                                                                                     || h.EndsWith(colName, StringComparison.OrdinalIgnoreCase));
                                    string value = string.Empty;
                                    if (hdrIndex >= 0)
                                    {
                                        var cell = row.Cell(hdrIndex + 1);
                                        value = cell?.GetString() ?? string.Empty;
                                    }
                                    dataRow[colName] = value;
                                    perMeterRow[colName] = value;
                                }

                                ReportDataTable.Rows.Add(dataRow);
                                dt.Rows.Add(perMeterRow);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to read Excel file {file}: {ex.Message}");
                    }
                }
            }

            TotalRowsLoaded = ReportDataTable.Rows.Count;
            OnPropertyChanged(nameof(ReportDataTable));
        }

        // helper to append rows from CSV into DataTable
        private void AppendRowsFromCsv(string csvPath, DataTable targetTable, IEnumerable<string> selectedColumns)
        {
            var lines = File.ReadAllLines(csvPath);
            if (lines.Length < 2) return;

            var headers = lines[0].Split(',').Select(h => NormalizeHeader(h).Trim()).ToArray();

            for (int i = 1; i < lines.Length; i++)
            {
                var rowParts = lines[i].Split(',');
                if (rowParts.Length != headers.Length)
                {
                    System.Diagnostics.Debug.WriteLine($"Row {i} length mismatch in {csvPath}: {rowParts.Length} vs {headers.Length}");
                    continue;
                }

                var dataRow = targetTable.NewRow();
                foreach (var col in selectedColumns)
                {
                    int idx = Array.FindIndex(headers, h => string.Equals(h, col, StringComparison.OrdinalIgnoreCase));
                    dataRow[col] = idx >= 0 ? rowParts[idx] : string.Empty;
                }
                targetTable.Rows.Add(dataRow);
            }
        }

        private void ExportToCsv()
        {
            if (ReportDataTable == null || ReportDataTable.Rows.Count == 0)
            {
                System.Windows.MessageBox.Show("No data to export.", "Export", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"Report_{SelectedReportFormat?.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var sb = new StringBuilder();
                // Header
                var columnNames = ReportDataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
                sb.AppendLine(string.Join(",", columnNames));
                // Rows
                foreach (DataRow row in ReportDataTable.Rows)
                {
                    var values = row.ItemArray.Select(v => EscapeCsv(v?.ToString() ?? ""));
                    sb.AppendLine(string.Join(",", values));
                }
                File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
                System.Windows.MessageBox.Show($"Exported to {saveDialog.FileName}", "Export Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private void ExportToExcel(Dictionary<string, DataTable> perMeterTables)
        {
            var saveDialog = new SaveFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = $"Report_{SelectedReportFormat?.Name}_{DateTime.Now:yyyyMMdd_HHmms}.xlsx" };
            if (saveDialog.ShowDialog() != true) return;

            using var wb = new XLWorkbook();
            foreach (var kv in perMeterTables)
            {
                var sheetName = string.IsNullOrWhiteSpace(kv.Key) ? "Meter" : kv.Key;
                wb.Worksheets.Add(kv.Value, sheetName.Length > 31 ? sheetName[..31] : sheetName);
            }
            wb.SaveAs(saveDialog.FileName);
            MessageBox.Show($"Exported to {saveDialog.FileName}", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

        // Helper added to normalize headers (strip sheet prefix like "[Meter]" if present)
        private static string NormalizeHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header)) return header ?? string.Empty;
            var h = header.Trim();
            var idx = h.IndexOf(']');
            if (idx >= 0 && h.StartsWith("["))
                return h.Substring(idx + 1).Trim();
            return h;
        }

    }
}
