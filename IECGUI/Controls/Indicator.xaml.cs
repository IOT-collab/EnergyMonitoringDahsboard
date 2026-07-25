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
    /// Interaction logic for Indicator.xaml
    /// </summary>
    public partial class Indicator : UserControl
    {
        public Indicator()
        {
            InitializeComponent();
        }


        public bool IsOn
        {
            get => (bool)GetValue(IsOnProperty);
            set => SetValue(IsOnProperty, value);
        }

        public static readonly DependencyProperty IsOnProperty =
            DependencyProperty.Register(
                nameof(IsOn),
                typeof(bool),
                typeof(Indicator),
                new PropertyMetadata(false));

        //------------------------------------------------

        public Color LampColor
        {
            get => (Color)GetValue(LampColorProperty);
            set => SetValue(LampColorProperty, value);
        }

        public static readonly DependencyProperty LampColorProperty =
            DependencyProperty.Register(
                nameof(LampColor),
                typeof(Color),
                typeof(Indicator),
                new PropertyMetadata(Colors.Lime));

        //------------------------------------------------

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(Indicator),
                new PropertyMetadata("Lamp"));

        //------------------------------------------------

        public double LampSize
        {
            get => (double)GetValue(LampSizeProperty);
            set => SetValue(LampSizeProperty, value);
        }

        public static readonly DependencyProperty LampSizeProperty =
            DependencyProperty.Register(
                nameof(LampSize),
                typeof(double),
                typeof(Indicator),
                new PropertyMetadata(55.0));
    }
}
