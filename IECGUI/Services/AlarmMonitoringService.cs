using IECGUI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace IECGUI.Services
{
    public class AlarmMonitoringService : ObservableObjectVM
    {
        private AlarmLogEntry? _currentPopupAlarm;
        private Visibility _alarmPopupVisibility = Visibility.Collapsed;

        public ObservableCollection<AlarmRule> Rules { get; } = new();
        public ObservableCollection<AlarmLogEntry> AlarmLogs { get; } = new();

        public ICommand AcknowledgeCurrentCommand { get; }
        public ICommand DismissPopupCommand { get; }
        public ICommand GenerateTestAlarmCommand { get; }
        public ICommand ClearLogsCommand { get; }

        public AlarmLogEntry? CurrentPopupAlarm
        {
            get => _currentPopupAlarm;
            set => SetProperty(ref _currentPopupAlarm, value);
        }

        public Visibility AlarmPopupVisibility
        {
            get => _alarmPopupVisibility;
            set => SetProperty(ref _alarmPopupVisibility, value);
        }

        public int ActiveAlarmCount => AlarmLogs.Count(x => x.State == AlarmState.Active || x.State == AlarmState.Acknowledged);
        public int CriticalAlarmCount => AlarmLogs.Count(x => x.Severity >= AlarmSeverity.Critical && (x.State == AlarmState.Active || x.State == AlarmState.Acknowledged));

        public AlarmMonitoringService()
        {
            SeedDefaultRules();

            AcknowledgeCurrentCommand = new RelayCommand(AcknowledgeCurrent);
            DismissPopupCommand = new RelayCommand(DismissPopup);
            GenerateTestAlarmCommand = new RelayCommand(GenerateTestAlarm);
            ClearLogsCommand = new RelayCommand(ClearLogs);
        }

        public void EvaluateReading(string meterName, IDictionary<string, double> values)
        {
            foreach (var rule in Rules.Where(x => x.IsEnabled && x.MeterName == meterName))
            {
                if (!values.TryGetValue(rule.ParameterName, out var value))
                    continue;

                var severity = ResolveSeverity(rule, value);
                if (severity == null)
                    continue;

                RaiseAlarm(rule, value, severity.Value);
            }
        }

        public void RaiseAlarm(AlarmRule rule, double value, AlarmSeverity severity)
        {
            var existing = AlarmLogs.FirstOrDefault(x =>
                x.State != AlarmState.Cleared &&
                x.MeterName == rule.MeterName &&
                x.ParameterName == rule.ParameterName &&
                x.Severity == severity);

            if (existing != null)
            {
                existing.Value = value;
                existing.Message = BuildMessage(rule, value, severity);
                OnPropertyChanged(nameof(ActiveAlarmCount));
                OnPropertyChanged(nameof(CriticalAlarmCount));
                return;
            }

            var alarm = new AlarmLogEntry
            {
                AlarmName = rule.AlarmName,
                MeterName = rule.MeterName,
                ParameterName = rule.ParameterName,
                Value = value,
                Unit = rule.Unit,
                Severity = severity,
                State = AlarmState.Active,
                Message = BuildMessage(rule, value, severity),
                RaisedAt = DateTime.Now
            };

            AlarmLogs.Insert(0, alarm);
            CurrentPopupAlarm = alarm;
            AlarmPopupVisibility = Visibility.Visible;
            OnPropertyChanged(nameof(ActiveAlarmCount));
            OnPropertyChanged(nameof(CriticalAlarmCount));
        }

        private AlarmSeverity? ResolveSeverity(AlarmRule rule, double value)
        {
            if (rule.HighAlarm.HasValue && value >= rule.HighAlarm.Value)
                return AlarmSeverity.Critical;

            if (rule.LowAlarm.HasValue && value <= rule.LowAlarm.Value)
                return AlarmSeverity.Critical;

            if (rule.HighWarning.HasValue && value >= rule.HighWarning.Value)
                return AlarmSeverity.Warning;

            if (rule.LowWarning.HasValue && value <= rule.LowWarning.Value)
                return AlarmSeverity.Warning;

            return null;
        }

        private static string BuildMessage(AlarmRule rule, double value, AlarmSeverity severity)
            => $"{rule.MeterName} {rule.ParameterName} {severity}: {value:F2} {rule.Unit}";

        private void AcknowledgeCurrent()
        {
            if (CurrentPopupAlarm != null)
            {
                CurrentPopupAlarm.IsAcknowledged = true;
                CurrentPopupAlarm.State = AlarmState.Acknowledged;
                CurrentPopupAlarm.AcknowledgedAt = DateTime.Now;
            }

            AlarmPopupVisibility = Visibility.Collapsed;
            OnPropertyChanged(nameof(ActiveAlarmCount));
            OnPropertyChanged(nameof(CriticalAlarmCount));
        }

        private void DismissPopup()
        {
            AlarmPopupVisibility = Visibility.Collapsed;
        }

        private void GenerateTestAlarm()
        {
            var rule = Rules.First(x => x.MeterName == "MFM-031" && x.ParameterName == "Current A");
            RaiseAlarm(rule, 512.6, AlarmSeverity.Critical);
        }

        private void ClearLogs()
        {
            foreach (var log in AlarmLogs)
            {
                log.State = AlarmState.Cleared;
                log.ClearedAt = DateTime.Now;
            }

            AlarmLogs.Clear();
            AlarmPopupVisibility = Visibility.Collapsed;
            CurrentPopupAlarm = null;
            OnPropertyChanged(nameof(ActiveAlarmCount));
            OnPropertyChanged(nameof(CriticalAlarmCount));
        }

        private void SeedDefaultRules()
        {
            Rules.Add(new AlarmRule
            {
                AlarmName = "Incomer 1 Current High",
                MeterName = "MFM-031",
                ParameterName = "Current A",
                Unit = "A",
                HighWarning = 450,
                HighAlarm = 500,
                DelaySeconds = 3,
                ResetMargin = 20
            });

            Rules.Add(new AlarmRule
            {
                AlarmName = "Incomer 1 Voltage Low",
                MeterName = "MFM-031",
                ParameterName = "Voltage A-N",
                Unit = "kV",
                LowWarning = 10.8,
                LowAlarm = 10.5,
                DelaySeconds = 3,
                ResetMargin = 0.2
            });

            Rules.Add(new AlarmRule
            {
                AlarmName = "Power Factor Low",
                MeterName = "MFM-031",
                ParameterName = "Power Factor",
                Unit = "PF",
                LowWarning = 0.9,
                LowAlarm = 0.85,
                DelaySeconds = 5,
                ResetMargin = 0.02
            });
        }
    }
}
