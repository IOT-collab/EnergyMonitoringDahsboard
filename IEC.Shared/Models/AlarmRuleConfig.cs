namespace IEC.Shared.Models
{
    public enum AlarmRuleKind
    {
        Numeric,
        Boolean,
        BreakerFeedbackMismatch
    }

    public enum AlarmComparisonOperator
    {
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Equal,
        NotEqual
    }

    public enum AlarmRuleSeverity
    {
        Information,
        Warning,
        Critical
    }

    public class AlarmRuleConfig
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString("N");
        public string AlarmName { get; set; } = string.Empty;
        public AlarmRuleKind RuleKind { get; set; } = AlarmRuleKind.Numeric;
        public string MeterName { get; set; } = string.Empty;
        public string ParameterName { get; set; } = string.Empty;
        public string BreakerKey { get; set; } = string.Empty;
        public AlarmComparisonOperator Operator { get; set; } = AlarmComparisonOperator.GreaterThanOrEqual;
        public double SetValue { get; set; }
        public bool ExpectedBoolean { get; set; } = true;
        public int DelaySeconds { get; set; } = 3;
        public AlarmRuleSeverity Severity { get; set; } = AlarmRuleSeverity.Warning;
        public bool IsEnabled { get; set; } = true;

        public string ExpressionText => RuleKind switch
        {
            AlarmRuleKind.Numeric => $"{MeterName} / {ParameterName} {OperatorText} {SetValue:G}",
            AlarmRuleKind.Boolean => $"{MeterName} / {ParameterName} = {ExpectedBoolean}",
            _ => $"{BreakerKey}: command != feedback"
        };

        private string OperatorText => Operator switch
        {
            AlarmComparisonOperator.GreaterThan => ">",
            AlarmComparisonOperator.GreaterThanOrEqual => ">=",
            AlarmComparisonOperator.LessThan => "<",
            AlarmComparisonOperator.LessThanOrEqual => "<=",
            AlarmComparisonOperator.Equal => "=",
            AlarmComparisonOperator.NotEqual => "!=",
            _ => "?"
        };
    }
}
