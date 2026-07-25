using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace IECGUI.Common.WPFExtensions.Controls
{
    public partial class OeeProgressBar : UserControl
    {
        public OeeProgressBar()
        {
            InitializeComponent();
            UpdateWidth();
        }

        // VALUE (e.g., 78%)
        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(OeeProgressBar),
                new PropertyMetadata(0.0, OnValueChanged));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((OeeProgressBar)d).UpdateWidth();
            ((OeeProgressBar)d).ValueText = $"{(double)e.NewValue:0.#}%";
        }

        // LABEL (Availability, Performance, etc.)
        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(OeeProgressBar));

        // VALUE TEXT (left)
        public string ValueText
        {
            get => (string)GetValue(ValueTextProperty);
            set => SetValue(ValueTextProperty, value);
        }
        public static readonly DependencyProperty ValueTextProperty =
            DependencyProperty.Register("ValueText", typeof(string), typeof(OeeProgressBar));

        // COLOR
        public Brush BarColor
        {
            get => (Brush)GetValue(BarColorProperty);
            set => SetValue(BarColorProperty, value);
        }
        public static readonly DependencyProperty BarColorProperty =
            DependencyProperty.Register("BarColor", typeof(Brush), typeof(OeeProgressBar),
                new PropertyMetadata(Brushes.SteelBlue));

        // INTERNAL — WIDTH OF THE FILLED BAR
        public double FillWidth
        {
            get => (double)GetValue(FillWidthProperty);
            set => SetValue(FillWidthProperty, value);
        }
        public static readonly DependencyProperty FillWidthProperty =
            DependencyProperty.Register("FillWidth", typeof(double), typeof(OeeProgressBar));

        private void UpdateWidth()
        {
            FillWidth = (Value / 100.0) * 280; // adjust bar width
        }
    }
}
