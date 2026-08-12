using IEC.CommonService;
using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IECGUI.Services
{
    public class AlarmMonitoringService : ObservableObjectVM
    {
        private readonly ConfigurationManagerService _configuration;
        private readonly IMultiEnergyMeterService _meters;
        private readonly SafePoller _poller;
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

        public AlarmLogEntry? CurrentPopupAlarm { get => _currentPopupAlarm; set => SetProperty(ref _currentPopupAlarm, value); }
        public Visibility AlarmPopupVisibility { get => _alarmPopupVisibility; set => SetProperty(ref _alarmPopupVisibility, value); }
        public int ActiveAlarmCount => AlarmLogs.Count(x => x.State is AlarmState.Active or AlarmState.Acknowledged);
        public int CriticalAlarmCount => AlarmLogs.Count(x => x.Severity >= AlarmSeverity.Critical && x.State is AlarmState.Active or AlarmState.Acknowledged);

        public AlarmMonitoringService(ConfigurationManagerService configuration, IMultiEnergyMeterService meters)
        {
            _configuration = configuration;
            _meters = meters;
            ReloadRules();
            AcknowledgeCurrentCommand = new RelayCommand(AcknowledgeCurrent);
            DismissPopupCommand = new RelayCommand(() => AlarmPopupVisibility = Visibility.Collapsed);
            GenerateTestAlarmCommand = new RelayCommand(GenerateTestAlarm);
            ClearLogsCommand = new RelayCommand(ClearLogs);
            _poller = new SafePoller(TimeSpan.FromSeconds(1), _ => PollAsync(), ex => Console.WriteLine($"Alarm polling: {ex.Message}"));
        }

        public async Task StartAsync()
        {
            var devices = _configuration.Configuration.Meters.Where(x => x.IsEnabled).ToList();
            await _meters.Configure(devices).ConfigureAwait(false);
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
                readings = await _meters.ReadAllAsync().ConfigureAwait(false);

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
                RuleId = rule.Id, AlarmName = rule.AlarmName, MeterName = rule.MeterName,
                ParameterName = rule.RuleKind == AlarmRuleKind.BreakerFeedbackMismatch ? rule.BreakerKey : rule.ParameterName,
                Value = value, Unit = unit, Severity = ToSeverity(rule.Severity),
                State = AlarmState.Active, Message = message, RaisedAt = DateTime.Now
            };
            AlarmLogs.Insert(0, alarm);
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
            }
            AlarmPopupVisibility = Visibility.Collapsed;
            NotifyCounts();
        }

        private void ClearLogs()
        {
            foreach (var log in AlarmLogs) { log.State = AlarmState.Cleared; log.ClearedAt = DateTime.Now; }
            AlarmLogs.Clear();
            AlarmPopupVisibility = Visibility.Collapsed;
            CurrentPopupAlarm = null;
            NotifyCounts();
        }

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
