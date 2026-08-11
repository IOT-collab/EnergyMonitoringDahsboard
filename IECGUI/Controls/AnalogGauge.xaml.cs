using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace IECGUI.Controls
{
    /// <summary>
    /// Interaction logic for AnalogGauge.xaml
    /// </summary>
    public partial class AnalogGauge : UserControl
    {
        public AnalogGauge()
        {
            InitializeComponent();
            UpdateNeedle();
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(AnalogGauge),
                new FrameworkPropertyMetadata(0.0, OnValueChanged, CoerceValue));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register(
                nameof(MinValue),
                typeof(double),
                typeof(AnalogGauge),
                new FrameworkPropertyMetadata(0.0, OnRangeChanged, CoerceMinimum));

        public double MinValue
        {
            get => (double)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register(
                nameof(MaxValue),
                typeof(double),
                typeof(AnalogGauge),
                new FrameworkPropertyMetadata(100.0, OnRangeChanged, CoerceMaximum));

        public double MaxValue
        {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(AnalogGauge),
                new PropertyMetadata("Gauge"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register(
                nameof(Unit),
                typeof(string),
                typeof(AnalogGauge),
                new PropertyMetadata(""));

        public string Unit
        {
            get => (string)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        private static void OnValueChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            ((AnalogGauge)d).UpdateNeedle();
        }

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var gauge = (AnalogGauge)d;
            gauge.CoerceValue(ValueProperty);
            gauge.UpdateNeedle();
        }

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            var gauge = (AnalogGauge)d;
            var value = (double)baseValue;
            var minimum = IsFinite(gauge.MinValue) ? gauge.MinValue : 0.0;
            var maximum = IsFinite(gauge.MaxValue) && gauge.MaxValue > minimum
                ? gauge.MaxValue
                : minimum + 1.0;

            if (!IsFinite(value))
                return minimum;

            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static object CoerceMinimum(DependencyObject d, object baseValue)
        {
            var value = (double)baseValue;
            return IsFinite(value) ? value : 0.0;
        }

        private static object CoerceMaximum(DependencyObject d, object baseValue)
        {
            var gauge = (AnalogGauge)d;
            var value = (double)baseValue;
            var minimum = IsFinite(gauge.MinValue) ? gauge.MinValue : 0.0;
            return IsFinite(value) && value > minimum ? value : minimum + 1.0;
        }

        private static bool IsFinite(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value);

        private void UpdateNeedle()
        {
            if (NeedleRotate == null)
                return;

            var minimum = IsFinite(MinValue) ? MinValue : 0.0;
            var maximum = IsFinite(MaxValue) && MaxValue > minimum ? MaxValue : minimum + 1.0;
            var safeValue = IsFinite(Value) ? Value : minimum;

            double percent = (safeValue - minimum) / (maximum - minimum);

            if (!IsFinite(percent))
                percent = 0.0;

            percent = Math.Max(0, Math.Min(1, percent));

            // 0..180 degrees
            double angle = -90 + (percent * 180);

            if (!IsFinite(angle))
                angle = -90;

            var animation = new DoubleAnimation
            {
                To = angle,
                Duration = TimeSpan.FromMilliseconds(650),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            NeedleRotate.BeginAnimation(RotateTransform.AngleProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }
    }
}

