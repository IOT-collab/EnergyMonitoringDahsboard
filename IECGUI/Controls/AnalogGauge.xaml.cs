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
                new PropertyMetadata(0.0, OnValueChanged));

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
                new PropertyMetadata(0.0));

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
                new PropertyMetadata(100.0));

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

        private void UpdateNeedle()
        {
            if (NeedleRotate == null)
                return;

            double percent =
                (Value - MinValue) /
                (MaxValue - MinValue);

            percent = Math.Max(0, Math.Min(1, percent));

            // 0..180 degrees
            double angle = -90 + (percent * 180);

            NeedleRotate.Angle = angle;
        }
    }
}

