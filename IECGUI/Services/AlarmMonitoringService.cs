using IEC.CommonService;
using IEC.Shared;
using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace IECGUI.Services
{
    public class AlarmMonitoringService : ObservableObjectVM
    {
        private readonly ConfigurationManagerService _configuration;
        private readonly IMultiEnergyMeterService _meters;
        private readonly DeviceRuntimeService _deviceRuntime;
        private readonly IAuthService _auth;
        private readonly SafePoller _poller;
        private readonly string _auditFile = Path.Combine(AppPaths.Logs, "AlarmAudit.csv");
        private readonly object _auditLock = new();
        private readonly Dictionary<string, DateTime> _conditionSince = new(StringComparer.OrdinalIgnoreCase);
        private bool _started;
        private AlarmLogEntry? _currentPopupAlarm;
        private Visibility _alarmPopupVisibility = Visibility.Collapsed;

        public ObservableCollection<AlarmRuleConfig> Rules { get; } = new();
        public ObservableCollection<AlarmLogEntry> AlarmLogs { get; } = new();

        public ICommand AcknowledgeCurrentCommand { get; }
        public ICommand DismissPopupCommand { get; }
        public ICommand GenerateTestAlarmCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand ExportCsvCommand { get; }

        public AlarmLogEntry? CurrentPopupAlarm { get => _currentPopupAlarm; set => SetProperty(ref _currentPopupAlarm, value); }
        public Visibility AlarmPopupVisibility { get => _alarmPopupVisibility; set => SetProperty(ref _alarmPopupVisibility, value); }
        public int ActiveAlarmCount => AlarmLogs.Count(x => x.State is AlarmState.Active or AlarmState.Acknowledged);
        public int CriticalAlarmCount => AlarmLogs.Count(x => x.Severity >= AlarmSeverity.Critical && x.State is AlarmState.Active or AlarmState.Acknowledged);

        public AlarmMonitoringService(ConfigurationManagerService configuration, IMultiEnergyMeterService meters, DeviceRuntimeService deviceRuntime, IAuthService auth)
        {
            _configuration = configuration;
            _meters = meters;
            _deviceRuntime = deviceRuntime;
            _auth = auth;
            ReloadRules();
            LoadAuditHistory();
            AcknowledgeCurrentCommand = new RelayCommand(AcknowledgeCurrent);
            DismissPopupCommand = new RelayCommand(() => AlarmPopupVisibility = Visibility.Collapsed);
            GenerateTestAlarmCommand = new RelayCommand(GenerateTestAlarm);
            ClearLogsCommand = new RelayCommand(ClearLogs);
            ExportCsvCommand = new RelayCommand(ExportCsv);
            _poller = new SafePoller(TimeSpan.FromSeconds(1), _ => PollAsync(), ex => Console.WriteLine($"Alarm polling: {ex.Message}"));
        }

        public async Task StartAsync()
        {
            await _deviceRuntime.StartAsync().ConfigureAwait(false);
            if (!_started)
            {
                _started = true;
                _poller.Start();
            }
            await PollAsync().ConfigureAwait(false);
        }

        public void ReloadRules()
        {
            Rules.Clear();
            foreach (var rule in _configuration.Configuration.AlarmRules)
                Rules.Add(rule);
        }

        public bool SaveRules()
        {
            _configuration.Configuration.AlarmRules = Rules.ToList();
            return _configuration.Save();
        }

        private async Task PollAsync()
        {
            var enabled = Rules.Where(x => x.IsEnabled).ToArray();
            if (enabled.Length == 0) return;

            Dictionary<string, MeterReading> readings = new(StringComparer.OrdinalIgnoreCase);
            if (enabled.Any(x => x.RuleKind != AlarmRuleKind.BreakerFeedbackMismatch))
                readings = _deviceRuntime.GetSnapshot();

            foreach (var rule in enabled)
            {
                try
                {
                    var result = rule.RuleKind == AlarmRuleKind.BreakerFeedbackMismatch
                        ? await EvaluateBreakerAsync(rule).ConfigureAwait(false)
                        : EvaluateParameter(rule, readings);
                    ApplyCondition(rule, result.IsActive, result.Value, result.Unit, result.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Alarm rule '{rule.AlarmName}': {ex.Message}");
                }
            }
        }

        private static (bool IsActive, double Value, string Unit, string Message) EvaluateParameter(
            AlarmRuleConfig rule, IDictionary<string, MeterReading> readings)
        {
            if (!readings.TryGetValue(rule.MeterName, out var reading) ||
                !reading.Values.TryGetValue(rule.ParameterName, out var raw) || raw == null)
                return (false, 0, string.Empty, "No live value");

            if (rule.RuleKind == AlarmRuleKind.Boolean)
            {
                var actual = raw is bool boolean ? boolean : Convert.ToDouble(raw, CultureInfo.InvariantCulture) != 0;
                return (actual == rule.ExpectedBoolean, actual ? 1 : 0, string.Empty,
                    $"{rule.MeterName} {rule.ParameterName} is {actual}");
            }

            var value = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
            if (double.IsNaN(value) || double.IsInfinity(value)) return (false, value, string.Empty, "Invalid live value");
            var active = rule.Operator switch
            {
                AlarmComparisonOperator.GreaterThan => value > rule.SetValue,
                AlarmComparisonOperator.GreaterThanOrEqual => value >= rule.SetValue,
                AlarmComparisonOperator.LessThan => value < rule.SetValue,
                AlarmComparisonOperator.LessThanOrEqual => value <= rule.SetValue,
                AlarmComparisonOperator.Equal => Math.Abs(value - rule.SetValue) < 0.000001,
                AlarmComparisonOperator.NotEqual => Math.Abs(value - rule.SetValue) >= 0.000001,
                _ => false
            };
            return (active, value, string.Empty, $"{rule.ExpressionText}; actual {value:G}");
        }

        private async Task<(bool IsActive, double Value, string Unit, string Message)> EvaluateBreakerAsync(AlarmRuleConfig rule)
        {
            var breaker = _configuration.Configuration.SldBreakers.FirstOrDefault(x =>
                string.Equals(x.BreakerKey, rule.BreakerKey, StringComparison.OrdinalIgnoreCase));
            if (breaker == null || !breaker.IsEnabled || string.IsNullOrWhiteSpace(breaker.MeterName))
                return (false, 0, string.Empty, "Breaker mapping unavailable");

            var command = await _meters.ReadBooleanAsync(breaker.MeterName, breaker.CommandArea, breaker.CommandAddress).ConfigureAwait(false);
            var feedback = await _meters.ReadBooleanAsync(breaker.MeterName, breaker.FeedbackArea, breaker.FeedbackAddress).ConfigureAwait(false);
            if (breaker.FeedbackInverted) feedback = !feedback;
            return (command != feedback, feedback ? 1 : 0, string.Empty,
                $"{breaker.DisplayName}: command {command}, feedback {feedback}");
        }

        private void ApplyCondition(AlarmRuleConfig rule, bool active, double value, string unit, string message)
        {
            if (!active)
            {
                _conditionSince.Remove(rule.Id);
                RunOnUi(() => ClearRuleAlarm(rule.Id));
                return;
            }

            if (!_conditionSince.TryGetValue(rule.Id, out var since))
            {
                _conditionSince[rule.Id] = DateTime.UtcNow;
                since = DateTime.UtcNow;
            }
            if ((DateTime.UtcNow - since).TotalSeconds < Math.Max(0, rule.DelaySeconds)) return;
            RunOnUi(() => RaiseAlarm(rule, value, unit, message));
        }

        private void RaiseAlarm(AlarmRuleConfig rule, double value, string unit, string message)
        {
            var existing = AlarmLogs.FirstOrDefault(x => x.RuleId == rule.Id && x.State != AlarmState.Cleared);
            if (existing != null) { existing.Value = value; existing.Message = message; return; }
            var alarm = new AlarmLogEntry
            {
                RuleId = rule.Id, Username = _auth.CurrentUser?.Username ?? "System",
                AlarmName = rule.AlarmName, MeterName = rule.MeterName,
                ParameterName = rule.RuleKind == AlarmRuleKind.BreakerFeedbackMismatch ? rule.BreakerKey : rule.ParameterName,
                Value = value, Unit = unit, Severity = ToSeverity(rule.Severity),
                State = AlarmState.Active, Message = message, RaisedAt = DateTime.Now
            };
            AlarmLogs.Insert(0, alarm);
            AppendAudit("Raised", alarm);
            CurrentPopupAlarm = alarm;
            AlarmPopupVisibility = Visibility.Visible;
            NotifyCounts();
        }

        private void ClearRuleAlarm(string ruleId)
        {
            var alarm = AlarmLogs.FirstOrDefault(x => x.RuleId == ruleId && x.State != AlarmState.Cleared);
            if (alarm == null) return;
            alarm.State = AlarmState.Cleared;
            alarm.ClearedAt = DateTime.Now;
            AppendAudit("Cleared", alarm);
            NotifyCounts();
        }

        private void GenerateTestAlarm()
        {
            var rule = Rules.FirstOrDefault();
            if (rule == null) return;
            RaiseAlarm(rule, rule.SetValue, string.Empty, $"Test alarm: {rule.ExpressionText}");
        }

        private void AcknowledgeCurrent()
        {
            if (CurrentPopupAlarm != null)
            {
                CurrentPopupAlarm.IsAcknowledged = true;
                CurrentPopupAlarm.State = AlarmState.Acknowledged;
                CurrentPopupAlarm.AcknowledgedAt = DateTime.Now;
                AppendAudit("Acknowledged", CurrentPopupAlarm);
            }
            AlarmPopupVisibility = Visibility.Collapsed;
            NotifyCounts();
        }

        private void ClearLogs()
        {
            foreach (var log in AlarmLogs.Where(x => x.State != AlarmState.Cleared))
            {
                log.State = AlarmState.Cleared;
                log.ClearedAt = DateTime.Now;
                AppendAudit("Cleared", log);
            }
            AlarmLogs.Clear();
            AlarmPopupVisibility = Visibility.Collapsed;
            CurrentPopupAlarm = null;
            NotifyCounts();
        }

        private void ExportCsv()
        {
            if (AlarmLogs.Count == 0)
            {
                MessageBox.Show("There is no alarm history to export.", "Alarm Export",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Export Alarm History",
                Filter = "CSV file (*.csv)|*.csv",
                DefaultExt = ".csv",
                AddExtension = true,
                FileName = $"AlarmHistory_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var csv = new StringBuilder();
                csv.AppendLine("Raised At,Alarm Name,Device,Parameter,Value,Unit,Severity,State,Username,Acknowledged At,Cleared At,Message");
                foreach (var alarm in AlarmLogs.OrderBy(x => x.RaisedAt))
                {
                    csv.AppendLine(string.Join(",", new[]
                    {
                        Csv(alarm.RaisedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                        Csv(alarm.AlarmName), Csv(alarm.MeterName), Csv(alarm.ParameterName),
                        Csv(alarm.Value.ToString("G", CultureInfo.InvariantCulture)), Csv(alarm.Unit),
                        Csv(alarm.SeverityText), Csv(alarm.StateText), Csv(alarm.Username),
                        Csv(alarm.AcknowledgedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty),
                        Csv(alarm.ClearedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty),
                        Csv(alarm.Message)
                    }));
                }
                File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(true));
                MessageBox.Show($"Alarm history exported successfully.\n\n{dialog.FileName}", "Alarm Export",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to export alarm history.\n\n{ex.Message}", "Alarm Export",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string Csv(string value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";

        private void AppendAudit(string action, AlarmLogEntry alarm)
        {
            try
            {
                lock (_auditLock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_auditFile)!);
                    if (!File.Exists(_auditFile))
                        File.WriteAllText(_auditFile,
                            "Audit Time,Action,Alarm ID,Rule ID,Raised At,Alarm Name,Device,Parameter,Value,Unit,Severity,State,Username,Actor,Acknowledged At,Cleared At,Message\r\n",
                            new UTF8Encoding(true));

                    var line = string.Join(",", new[]
                    {
                        Csv(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                        Csv(action), Csv(alarm.AlarmId), Csv(alarm.RuleId),
                        Csv(alarm.RaisedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)),
                        Csv(alarm.AlarmName), Csv(alarm.MeterName), Csv(alarm.ParameterName),
                        Csv(alarm.Value.ToString("G", CultureInfo.InvariantCulture)), Csv(alarm.Unit),
                        Csv(alarm.SeverityText), Csv(alarm.StateText), Csv(alarm.Username),
                        Csv(_auth.CurrentUser?.Username ?? "System"),
                        Csv(alarm.AcknowledgedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty),
                        Csv(alarm.ClearedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty),
                        Csv(alarm.Message)
                    });
                    File.AppendAllText(_auditFile, line + Environment.NewLine, new UTF8Encoding(false));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Alarm audit write: {ex.Message}");
            }
        }

        private void LoadAuditHistory()
        {
            if (!File.Exists(_auditFile)) return;
            try
            {
                var alarms = new Dictionary<string, AlarmLogEntry>(StringComparer.OrdinalIgnoreCase);
                foreach (var line in File.ReadLines(_auditFile).Skip(1))
                {
                    var fields = ParseCsv(line);
                    if (fields.Count < 17 || string.IsNullOrWhiteSpace(fields[2])) continue;
                    var id = fields[2];
                    if (!alarms.TryGetValue(id, out var alarm))
                    {
                        alarm = new AlarmLogEntry
                        {
                            AlarmId = id,
                            RuleId = fields[3],
                            RaisedAt = ParseDate(fields[4]) ?? DateTime.Now,
                            AlarmName = fields[5], MeterName = fields[6], ParameterName = fields[7],
                            Value = ParseDouble(fields[8]), Unit = fields[9],
                            Severity = Enum.TryParse<AlarmSeverity>(fields[10], true, out var severity) ? severity : AlarmSeverity.Warning,
                            Username = fields[12], Message = fields[16]
                        };
                        alarms[id] = alarm;
                    }

                    alarm.Value = ParseDouble(fields[8]);
                    alarm.Message = fields[16];
                    alarm.State = Enum.TryParse<AlarmState>(fields[11], true, out var state) ? state : AlarmState.Active;
                    alarm.AcknowledgedAt = ParseDate(fields[14]);
                    alarm.ClearedAt = ParseDate(fields[15]);
                    alarm.IsAcknowledged = alarm.State == AlarmState.Acknowledged;
                }

                foreach (var alarm in alarms.Values.OrderByDescending(x => x.RaisedAt))
                    AlarmLogs.Add(alarm);
                NotifyCounts();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Alarm audit read: {ex.Message}");
            }
        }

        private static List<string> ParseCsv(string line)
        {
            var values = new List<string>();
            var value = new StringBuilder();
            var quoted = false;
            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"') { value.Append('"'); i++; }
                    else quoted = !quoted;
                }
                else if (ch == ',' && !quoted) { values.Add(value.ToString()); value.Clear(); }
                else value.Append(ch);
            }
            values.Add(value.ToString());
            return values;
        }

        private static DateTime? ParseDate(string value) =>
            DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var result) ? result : null;
        private static double ParseDouble(string value) =>
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : 0;

        private void NotifyCounts() { OnPropertyChanged(nameof(ActiveAlarmCount)); OnPropertyChanged(nameof(CriticalAlarmCount)); }
        private static AlarmSeverity ToSeverity(AlarmRuleSeverity severity) => severity switch
        {
            AlarmRuleSeverity.Information => AlarmSeverity.Info,
            AlarmRuleSeverity.Warning => AlarmSeverity.Warning,
            AlarmRuleSeverity.Critical => AlarmSeverity.Critical,
            _ => AlarmSeverity.Warning
        };
        private static void RunOnUi(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess()) action(); else dispatcher.Invoke(action);
        }
    }
}
