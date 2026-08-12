using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IECGUI.ViewModel
{
    public class AlarmViewModel : BaseViewModel
    {
        private readonly INavigationService _navigation;
        private readonly ConfigurationManagerService _configuration;
        private AlarmRuleConfig? _selectedRule;
        private string _alarmName = string.Empty;
        private AlarmRuleKind _selectedRuleKind;
        private string? _selectedMeterName;
        private string? _selectedParameterName;
        private string? _selectedBreakerKey;
        private AlarmComparisonOperator _selectedOperator = AlarmComparisonOperator.GreaterThanOrEqual;
        private double _setValue;
        private bool _expectedBoolean = true;
        private int _delaySeconds = 3;
        private AlarmRuleSeverity _selectedSeverity = AlarmRuleSeverity.Warning;
        private bool _isRuleEnabled = true;
        private string _configurationStatus = "Ready";

        public AlarmMonitoringService AlarmService { get; }
        public ObservableCollection<string> MeterNames { get; } = new();
        public ObservableCollection<string> ParameterNames { get; } = new();
        public ObservableCollection<string> BreakerKeys { get; } = new();
        public Array RuleKinds => Enum.GetValues(typeof(AlarmRuleKind));
        public Array Operators => Enum.GetValues(typeof(AlarmComparisonOperator));
        public Array Severities => Enum.GetValues(typeof(AlarmRuleSeverity));
        public ICommand BackCommand { get; }
        public ICommand NewRuleCommand { get; }
        public ICommand SaveRuleCommand { get; }
        public ICommand DeleteRuleCommand { get; }

        public AlarmRuleConfig? SelectedRule
        {
            get => _selectedRule;
            set { if (SetProperty(ref _selectedRule, value) && value != null) LoadEditor(value); }
        }
        public string AlarmName { get => _alarmName; set => SetProperty(ref _alarmName, value); }
        public AlarmRuleKind SelectedRuleKind { get => _selectedRuleKind; set => SetProperty(ref _selectedRuleKind, value); }
        public string? SelectedMeterName
        {
            get => _selectedMeterName;
            set { if (SetProperty(ref _selectedMeterName, value)) RefreshParameters(); }
        }
        public string? SelectedParameterName { get => _selectedParameterName; set => SetProperty(ref _selectedParameterName, value); }
        public string? SelectedBreakerKey { get => _selectedBreakerKey; set => SetProperty(ref _selectedBreakerKey, value); }
        public AlarmComparisonOperator SelectedOperator { get => _selectedOperator; set => SetProperty(ref _selectedOperator, value); }
        public double SetValue { get => _setValue; set => SetProperty(ref _setValue, value); }
        public bool ExpectedBoolean { get => _expectedBoolean; set => SetProperty(ref _expectedBoolean, value); }
        public int DelaySeconds { get => _delaySeconds; set => SetProperty(ref _delaySeconds, Math.Max(0, value)); }
        public AlarmRuleSeverity SelectedSeverity { get => _selectedSeverity; set => SetProperty(ref _selectedSeverity, value); }
        public bool IsRuleEnabled { get => _isRuleEnabled; set => SetProperty(ref _isRuleEnabled, value); }
        public string ConfigurationStatus { get => _configurationStatus; set => SetProperty(ref _configurationStatus, value); }

        public AlarmViewModel(INavigationService navigation, AlarmMonitoringService alarmService, ConfigurationManagerService configuration)
        {
            _navigation = navigation;
            AlarmService = alarmService;
            _configuration = configuration;
            foreach (var meter in configuration.Configuration.Meters.Where(x => x.IsEnabled)) MeterNames.Add(meter.MeterName);
            foreach (var breaker in configuration.Configuration.SldBreakers.Where(x => x.IsEnabled)) BreakerKeys.Add(breaker.BreakerKey);
            BackCommand = new RelayCommand(() => _navigation.NavigateTo<HomePageViewModel>());
            NewRuleCommand = new RelayCommand(NewRule);
            SaveRuleCommand = new RelayCommand(SaveRule);
            DeleteRuleCommand = new RelayCommand(DeleteRule);
            NewRule();
            _ = StartMonitoringAsync();
        }

        private async Task StartMonitoringAsync()
        {
            try { await AlarmService.StartAsync(); ConfigurationStatus = "Live alarm evaluation running"; }
            catch (Exception ex) { ConfigurationStatus = $"Alarm monitor unavailable: {ex.Message}"; }
        }

        private void RefreshParameters()
        {
            var previous = SelectedParameterName;
            ParameterNames.Clear();
            var meter = _configuration.Configuration.Meters.FirstOrDefault(x =>
                string.Equals(x.MeterName, SelectedMeterName, StringComparison.OrdinalIgnoreCase));
            if (meter?.Registers != null)
                foreach (var register in meter.Registers.Where(x => x.IsEnabled && !string.IsNullOrWhiteSpace(x.ParameterName)))
                    ParameterNames.Add(register.ParameterName);
            SelectedParameterName = ParameterNames.Contains(previous ?? string.Empty) ? previous : ParameterNames.FirstOrDefault();
        }

        private void NewRule()
        {
            SelectedRule = null;
            AlarmName = string.Empty;
            SelectedRuleKind = AlarmRuleKind.Numeric;
            SelectedMeterName = MeterNames.FirstOrDefault();
            SelectedParameterName = ParameterNames.FirstOrDefault();
            SelectedBreakerKey = BreakerKeys.FirstOrDefault();
            SelectedOperator = AlarmComparisonOperator.GreaterThanOrEqual;
            SetValue = 0;
            ExpectedBoolean = true;
            DelaySeconds = 3;
            SelectedSeverity = AlarmRuleSeverity.Warning;
            IsRuleEnabled = true;
            ConfigurationStatus = "New rule";
        }

        private void SaveRule()
        {
            if (string.IsNullOrWhiteSpace(AlarmName)) { ConfigurationStatus = "Alarm name is required"; return; }
            if (SelectedRuleKind != AlarmRuleKind.BreakerFeedbackMismatch &&
                (string.IsNullOrWhiteSpace(SelectedMeterName) || string.IsNullOrWhiteSpace(SelectedParameterName)))
            { ConfigurationStatus = "Select a device and parameter"; return; }
            if (SelectedRuleKind == AlarmRuleKind.BreakerFeedbackMismatch && string.IsNullOrWhiteSpace(SelectedBreakerKey))
            { ConfigurationStatus = "Select an SLD breaker mapping"; return; }

            var rule = SelectedRule ?? new AlarmRuleConfig();
            rule.AlarmName = AlarmName.Trim();
            rule.RuleKind = SelectedRuleKind;
            rule.MeterName = SelectedMeterName ?? string.Empty;
            rule.ParameterName = SelectedParameterName ?? string.Empty;
            rule.BreakerKey = SelectedBreakerKey ?? string.Empty;
            rule.Operator = SelectedOperator;
            rule.SetValue = SetValue;
            rule.ExpectedBoolean = ExpectedBoolean;
            rule.DelaySeconds = DelaySeconds;
            rule.Severity = SelectedSeverity;
            rule.IsEnabled = IsRuleEnabled;
            if (SelectedRule == null) AlarmService.Rules.Add(rule);
            AlarmService.SaveRules();
            AlarmService.ReloadRules();
            SelectedRule = AlarmService.Rules.FirstOrDefault(x => x.Id == rule.Id);
            ConfigurationStatus = "Alarm rule saved";
        }

        private void DeleteRule()
        {
            if (SelectedRule == null) return;
            AlarmService.Rules.Remove(SelectedRule);
            AlarmService.SaveRules();
            NewRule();
            ConfigurationStatus = "Alarm rule deleted";
        }

        private void LoadEditor(AlarmRuleConfig rule)
        {
            AlarmName = rule.AlarmName;
            SelectedRuleKind = rule.RuleKind;
            SelectedMeterName = rule.MeterName;
            SelectedParameterName = rule.ParameterName;
            SelectedBreakerKey = rule.BreakerKey;
            SelectedOperator = rule.Operator;
            SetValue = rule.SetValue;
            ExpectedBoolean = rule.ExpectedBoolean;
            DelaySeconds = rule.DelaySeconds;
            SelectedSeverity = rule.Severity;
            IsRuleEnabled = rule.IsEnabled;
        }
    }
}
