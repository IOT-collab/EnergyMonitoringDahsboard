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


namespace IECGUI.ViewModel
{
    public class ReportViewerViewModel : BaseViewModel
    {
        public ObservableCollection<ReportFormatConfig> ReportFormats { get; set; } = new();

        private readonly INavigationService _navigation;


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
        public DataTable ReportDataTable { get; set; } = new DataTable();

        private readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ReportFormats.json");
        private string _prodCsvFolder;

        public ReportViewerViewModel(INavigationService navigation)
        {
            LoadReportFormats();
            LoadDataCommand = new RelayCommand(LoadData);
            ExportCsvCommand = new RelayCommand(ExportToCsv);
            RefreshFormatsCommand = new RelayCommand(RefreshFormats);
            MenuCommand = new RelayCommand(() => _navigation.NavigateTo<HomePageViewModel>());
            _navigation = navigation;
            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            _prodCsvFolder = Path.Combine(appFolder, "Reports.csv");
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
            TotalRowsLoaded = 0;

            if (SelectedReportFormat == null || SelectedReportFormat.SelectedColumns == null || !SelectedReportFormat.SelectedColumns.Any())
            {
                MessageBox.Show("No report format or columns selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                OnPropertyChanged(nameof(ReportDataTable));
                return;
            }

            // Add columns to DataTable
            foreach (var col in SelectedReportFormat.SelectedColumns)
            {
                ReportDataTable.Columns.Add(col);
            }

            for (var date = DateFrom.Date; date <= DateTo.Date; date = date.AddDays(1))
            {
                var filePath = Path.Combine(_prodCsvFolder, $"Production_{date:yyyyMMdd}.csv");
                System.Diagnostics.Debug.WriteLine($"Looking for file: {filePath}");
                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine("File not found.");
                    continue;
                }
                var lines = File.ReadAllLines(filePath);
                if (lines.Length < 2)
                {
                    System.Diagnostics.Debug.WriteLine("File has no data.");
                    continue;
                }
                var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
                System.Diagnostics.Debug.WriteLine($"Headers ({headers.Length}): {string.Join(" | ", headers)}");
                for (int i = 1; i < lines.Length; i++)
                {
                    var row = lines[i].Split(',');
                    if (row.Length != headers.Length)
                    {
                        System.Diagnostics.Debug.WriteLine($"Row {i} length mismatch: {row.Length} vs {headers.Length}");
                        continue;
                    }
                    var dataRow = ReportDataTable.NewRow();
                    foreach (var col in SelectedReportFormat.SelectedColumns)
                    {
                        int idx = Array.IndexOf(headers, col);
                        if (idx >= 0)
                            dataRow[col] = row[idx];
                        else
                            dataRow[col] = string.Empty;
                    }
                    ReportDataTable.Rows.Add(dataRow);
                }
            }

            TotalRowsLoaded = ReportDataTable.Rows.Count;
            System.Diagnostics.Debug.WriteLine($"Total rows loaded: {TotalRowsLoaded}");
            OnPropertyChanged(nameof(ReportDataTable));
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

        private string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

    }
}
