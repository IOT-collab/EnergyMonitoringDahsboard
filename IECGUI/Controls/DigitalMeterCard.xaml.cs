using Newtonsoft.Json.Linq;
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
    /// Interaction logic for DigitalMeterCard.xaml
    /// </summary>
    public partial class DigitalMeterCard : UserControl
    {
        public DigitalMeterCard()
        {
            InitializeComponent();
        }


        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register( nameof(Value), typeof(string),typeof(DigitalMeterCard), new PropertyMetadata(""));
        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }


        public static readonly DependencyProperty CurrentBrushProperty = DependencyProperty.Register(nameof(CurrentBrush), typeof(string), typeof(DigitalMeterCard), new PropertyMetadata(""));
        public string CurrentBrush
        {
            get => (string)GetValue(CurrentBrushProperty);
            set => SetValue(CurrentBrushProperty, value);
        }
    }
}
