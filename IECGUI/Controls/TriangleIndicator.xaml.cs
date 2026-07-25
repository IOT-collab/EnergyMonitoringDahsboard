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
    /// Interaction logic for TriangleIndicator.xaml
    /// </summary>
    public partial class TriangleIndicator : UserControl
    {
        public TriangleIndicator()
        {
            InitializeComponent();


            SizeChanged += (_, __) => UpdatePoints();
            Loaded += (_, __) => UpdatePoints();
        }

        #region Dependency Properties

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(
                nameof(Fill),
                typeof(Brush),
                typeof(TriangleIndicator),
                new PropertyMetadata(Brushes.LimeGreen));

        public Brush Fill
        {
            get => (Brush)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        //--------------------------------------------------------

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register(
                nameof(Stroke),
                typeof(Brush),
                typeof(TriangleIndicator),
                new PropertyMetadata(Brushes.Black));

        public Brush Stroke
        {
            get => (Brush)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        //--------------------------------------------------------

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(TriangleIndicator),
                new PropertyMetadata(2.0));

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        //--------------------------------------------------------

        public static readonly DependencyProperty DirectionProperty =
            DependencyProperty.Register(
                nameof(Direction),
                typeof(TriangleDirection),
                typeof(TriangleIndicator),
                new PropertyMetadata(TriangleDirection.Up, OnDirectionChanged));

        public TriangleDirection Direction
        {
            get => (TriangleDirection)GetValue(DirectionProperty);
            set => SetValue(DirectionProperty, value);
        }

        private static void OnDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TriangleIndicator)d).UpdatePoints();
        }

        //--------------------------------------------------------

        public static readonly DependencyProperty TrianglePointsProperty =
            DependencyProperty.Register(
                nameof(TrianglePoints),
                typeof(PointCollection),
                typeof(TriangleIndicator));

        public PointCollection TrianglePoints
        {
            get => (PointCollection)GetValue(TrianglePointsProperty);
            set => SetValue(TrianglePointsProperty, value);
        }

        #endregion

        //--------------------------------------------------------

        private void UpdatePoints()
        {
            double w = ActualWidth;
            double h = ActualHeight;
                     

            if (w <= 0 || h <= 0)
                return;

            switch (Direction)
            {
                case TriangleDirection.Up:
                    PART_Triangle.Points = new PointCollection()
                    {
                        new Point(w/2,0),
                        new Point(0,h),
                        new Point(w,h)
                    };
                    break;

                case TriangleDirection.Down:
                    PART_Triangle.Points = new PointCollection()
                    {
                        new Point(0,0),
                        new Point(w,0),
                        new Point(w/2,h)
                    };
                    break;

                case TriangleDirection.Left:
                    PART_Triangle.Points = new PointCollection()
                    {
                        new Point(0,h/2),
                        new Point(w,0),
                        new Point(w,h)
                    };
                    break;

                case TriangleDirection.Right:
                    PART_Triangle.Points = new PointCollection()
                    {
                        new Point(0,0),
                        new Point(w,h/2),
                        new Point(0,h)
                    };
                    break;
            }


         }

        public enum TriangleDirection
        {
            Up,
            Down,
            Left,
            Right
        }
    }
}
