using IEC.Shared;
using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.Converters;
using IECGUI.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows.Input;

namespace IECGUI.ViewModel
{
    public class ReportConfigViewModel : BaseViewModel
    {
        private readonly INavigationService _navigation;
        private readonly ConfigurationManagerService _configuration;
        public ObservableCollection<ReportFormatConfig> ReportFormats { get; set; } = new();
        public ObservableCollection<string> AvailableColumns { get; set; } = new();
        public ObservableCollection<string> SelectedColumns { get; set; } = new();
        public ObservableCollection<string> MeterNames { get; } = new();
        public IEnumerable<string> SelectedColumnsView
            => SelectedFormat != null ? SelectedFormat.SelectedColumns : SelectedColumns;

        private string _reportName;
        public string ReportName
        {
            get => _reportName;
            set { _reportName = value; OnPropertyChanged(nameof(ReportName)); }
        }

        private string _selectedMeterName;
        public string SelectedMeterName
        {
            get => _selectedMeterName;
            set
            {
                if (string.Equals(_selectedMeterName, value, System.StringComparison.Ordinal)) return;
                _selectedMeterName = value;
                OnPropertyChanged(nameof(SelectedMeterName));
                RefreshAvailableColumns(value);
                SelectedColumns.Clear();
            }
        }

        private ReportFormatConfig _selectedFormat;
        public ReportFormatConfig SelectedFormat
        {
            get => _selectedFormat;
            set
            {
                _selectedFormat = value;
                OnPropertyChanged(nameof(SelectedFormat));
                OnPropertyChanged(nameof(SelectedColumnsView));
                ReportName = value?.Name ?? string.Empty;
                SelectedMeterName = value?.MeterName;
                SelectedColumns.Clear();
                if (value != null && value.SelectedColumns != null)
                {
                    foreach (var col in value.SelectedColumns)
                    {
                        // If saved column contains sheet prefix like "[Meter]VoltageA", normalize to show matching available item when possible
                        var normalized = NormalizeSavedColumn(col);
                        // prefer exact AvailableColumns entry if present
                        var match = AvailableColumns.FirstOrDefault(a => string.Equals(a, normalized, System.StringComparison.OrdinalIgnoreCase))
                                    ?? AvailableColumns.FirstOrDefault(a => normalized.EndsWith(a, System.StringComparison.OrdinalIgnoreCase))
                                    ?? normalized;
                        SelectedColumns.Add(match);
                    }
                    // reflect only valid selections in the stored selected set
                    value.SelectedColumns = SelectedColumns.ToList();
                }
                OnPropertyChanged(nameof(SelectedColumns));
            }
        }

        private string _selectedColumnToRemove;
        public string SelectedColumnToRemove
        {
            get => _selectedColumnToRemove;
            set { _selectedColumnToRemove = value; OnPropertyChanged(nameof(SelectedColumnToRemove)); }
        }

        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand RemoveColumnCommand { get; }

        public ICommand BackCommand { get; }

        private readonly string _configPath = AppPaths.ReportFormatFile;

        public ReportConfigViewModel(INavigationService navigation, ConfigurationManagerService config)
        {
            _navigation = navigation;
            _configuration = config;

            foreach (var meterName in (config.Configuration?.Meters ?? new List<MetersConfig>())
                // Keep disabled meters available because their historical reports
                // must remain accessible after they are taken out of live polling.
                .Where(m => m != null && !string.IsNullOrWhiteSpace(m.MeterName))
                .Select(m => m.MeterName.Trim())
                .Distinct(System.StringComparer.OrdinalIgnoreCase))
            {
                MeterNames.Add(meterName);
            }

            AvailableColumns = new ObservableCollection<string>();
            SaveCommand = new RelayCommand(SaveFormat);
            DeleteCommand = new RelayCommand(DeleteFormat, () => SelectedFormat != null);
            NewCommand = new RelayCommand(NewFormat);
            EditCommand = new RelayCommand(EditFormat, () => SelectedFormat != null);
            BackCommand = new RelayCommand(() => _navigation.NavigateTo<ReportViewerViewModel>());
            RemoveColumnCommand = new RelayCommand(RemoveSelectedColumn, () => SelectedColumnToRemove != null);

            LoadReportFormats();

            // Older formats did not store a meter. Assign the first configured
            // meter so they remain usable, then persist the migration.
            var meterMigration = false;
            foreach (var format in ReportFormats.Where(f => string.IsNullOrWhiteSpace(f.MeterName)))
            {
                format.MeterName = MeterNames.FirstOrDefault();
                meterMigration = true;
            }

            SelectedMeterName = MeterNames.FirstOrDefault();

            // Clean existing saved formats so they only contain columns that match current AvailableColumns
            bool changed = CleanAndNormalizeSavedFormats();
            if (changed || meterMigration) SaveReportFormats();

            SelectedColumns.CollectionChanged += (s, e) => OnPropertyChanged(nameof(SelectedColumnsView));
        }

        private bool CleanAndNormalizeSavedFormats()
        {
            bool anyChange = false;
            foreach (var fmt in ReportFormats)
            {
                if (fmt.SelectedColumns == null) continue;
                var availableForMeter = GetAvailableColumns(fmt.MeterName);
                var original = fmt.SelectedColumns.ToList();
                var newList = new List<string>();
                foreach (var col in original)
                {
                    var normalized = NormalizeSavedColumn(col);
                    var match = availableForMeter.FirstOrDefault(a => string.Equals(a, normalized, System.StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        newList.Add(match);
                    }
                    // else drop unknown legacy column
                }

                if (!newList.SequenceEqual(original, StringComparer.OrdinalIgnoreCase))
                {
                    fmt.SelectedColumns = newList;
                    anyChange = true;
                }
            }
            return anyChange;
        }

        private List<string> GetAvailableColumns(string meterName)
        {
            var columns = new List<string> { "Timestamp" };
            var meter = (_configuration.Configuration?.Meters ?? new List<MetersConfig>())
                .FirstOrDefault(m => string.Equals(m?.MeterName, meterName, System.StringComparison.OrdinalIgnoreCase));

            if (meter?.Registers != null)
            {
                columns.AddRange(meter.Registers
                    .Where(r => r != null && r.IsEnabled)
                    .Select(r => string.IsNullOrWhiteSpace(r.ParameterName)
                        ? $"Register {r.RegisterAddress}"
                        : r.ParameterName.Trim())
                    .Distinct(System.StringComparer.OrdinalIgnoreCase));
            }

            return columns;
        }

        private void RefreshAvailableColumns(string meterName)
        {
            AvailableColumns.Clear();
            foreach (var column in GetAvailableColumns(meterName))
                AvailableColumns.Add(column);
            OnPropertyChanged(nameof(AvailableColumns));
        }

        private static string NormalizeSavedColumn(string saved)
        {
            if (string.IsNullOrWhiteSpace(saved)) return saved ?? string.Empty;
            // If saved column contains a sheet prefix like "[Meter]VoltageA", strip prefix
            var idx = saved.IndexOf(']');
            if (idx >= 0 && saved.StartsWith("["))
            {
                return saved.Substring(idx + 1).Trim();
            }
            return saved.Trim();
        }

        private void SaveFormat()
        {
            if (string.IsNullOrWhiteSpace(ReportName) ||
                string.IsNullOrWhiteSpace(SelectedMeterName) ||
                !SelectedColumns.Any()) return;
            var existing = ReportFormats.FirstOrDefault(f => f.Name == ReportName);
            if (existing != null)
            {
                existing.MeterName = SelectedMeterName;
                existing.SelectedColumns = SelectedColumns.ToList();
            }
            else
            {
                ReportFormats.Add(new ReportFormatConfig
                {
                    Name = ReportName,
                    MeterName = SelectedMeterName,
                    SelectedColumns = SelectedColumns.ToList()
                });
            }
            SelectedFormat = ReportFormats.FirstOrDefault(f => f.Name == ReportName);
            SaveReportFormats();
            OnPropertyChanged(nameof(SelectedColumnsView));
        }

        private void DeleteFormat()
        {
            if (SelectedFormat != null)
            {
                ReportFormats.Remove(SelectedFormat);
                SelectedFormat = null;
                ReportName = string.Empty;
                SelectedColumns.Clear();
                SaveReportFormats();
                OnPropertyChanged(nameof(SelectedColumnsView));
            }
        }

        private void NewFormat()
        {
            SelectedFormat = null;
            ReportName = string.Empty;
            SelectedMeterName = MeterNames.FirstOrDefault();
            SelectedColumns.Clear();
            OnPropertyChanged(nameof(SelectedColumnsView));
        }

        private void EditFormat()
        {
            if (SelectedFormat != null)
            {
                ReportName = SelectedFormat.Name;
                SelectedMeterName = SelectedFormat.MeterName;
                SelectedColumns.Clear();
                foreach (var col in SelectedFormat.SelectedColumns)
                    SelectedColumns.Add(NormalizeSavedColumn(col));
                OnPropertyChanged(nameof(SelectedColumnsView));
            }
        }

        private void RemoveSelectedColumn()
        {
            if (SelectedFormat != null && SelectedColumnToRemove != null)
            {
                SelectedFormat.SelectedColumns.Remove(SelectedColumnToRemove);
                if (SelectedColumns.Contains(SelectedColumnToRemove))
                    SelectedColumns.Remove(SelectedColumnToRemove);
                SelectedColumnToRemove = null;
                SaveReportFormats();
                OnPropertyChanged(nameof(SelectedColumnsView));
            }
            else if (SelectedFormat == null && SelectedColumnToRemove != null)
            {
                if (SelectedColumns.Contains(SelectedColumnToRemove))
                    SelectedColumns.Remove(SelectedColumnToRemove);
                SelectedColumnToRemove = null;
                OnPropertyChanged(nameof(SelectedColumnsView));
            }
        }

        private void SaveReportFormats()
        {
            try
            {
                var dir = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(ReportFormats.ToList());
                File.WriteAllText(_configPath, json);
            }
            catch { /* handle/log error if needed */ }
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
            catch { /* handle/log error if needed */ }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
