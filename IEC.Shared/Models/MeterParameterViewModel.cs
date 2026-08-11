using System;
using System.Globalization;

namespace IEC.Shared.Models
{
    /// <summary>
    /// One configured register rendered on a meter card. This allows every
    /// meter to expose its own parameter set instead of relying on fixed fields.
    /// </summary>
    public class MeterParameterViewModel : ObservableObjectVM
    {
        private double? _value;

        public string ParameterName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public ushort RegisterAddress { get; set; }

        public double? Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                    OnPropertyChanged(nameof(DisplayValue));
            }
        }

        public string DisplayValue
        {
            get
            {
                if (!Value.HasValue || double.IsNaN(Value.Value) || double.IsInfinity(Value.Value))
                    return "--";

                return Value.Value.ToString("0.###", CultureInfo.InvariantCulture);
            }
        }
    }
}
