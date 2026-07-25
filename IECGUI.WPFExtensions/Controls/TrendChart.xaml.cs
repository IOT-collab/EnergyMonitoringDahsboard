using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace IECGUI.Common.WPFExtensions.Controls
{
    public partial class TrendChart : UserControl
    {
        private readonly DispatcherTimer _debounceTimer;

        // PADDING SETTINGS (To stop text merging)
        private const double PadLeft = 40;   // Space for Y-Axis Text (Seconds)
        private const double PadBottom = 25; // Space for X-Axis Text (Days)
        private const double PadTop = 10;    // Space at top so max value isn't cut off
        private const double PadRight = 10;  // Space at right

        public TrendChart()
        {
            InitializeComponent();

            // 1. Debounce Logic (Fixes Flickering)
            // Only redraw if no resize events happen for 100ms
            _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _debounceTimer.Tick += (s, e) =>
            {
                _debounceTimer.Stop();
                Redraw();
            };

            SizeChanged += (s, e) =>
            {
                _debounceTimer.Stop();
                _debounceTimer.Start(); // Restart timer on every size change
            };

            Loaded += (s, e) => Redraw();

           // PlotCanvas.MouseMove += PlotCanvas_MouseMove;
            //PlotCanvas.MouseLeave += (_, __) => TooltipBox.Visibility = Visibility.Collapsed;
        }

        // ===================== Dependency Properties =====================
        public IEnumerable<double> Values
        {
            get => (IEnumerable<double>)GetValue(ValuesProperty);
            set => SetValue(ValuesProperty, value);
        }
        public static readonly DependencyProperty ValuesProperty =
            DependencyProperty.Register(nameof(Values), typeof(IEnumerable<double>),
            typeof(TrendChart), new PropertyMetadata(null, OnValuesChanged));

        private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Immediate redraw when DATA changes (not size)
            ((TrendChart)d).Redraw();
        }

        // Keep other properties standard...
        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(TrendChart), new PropertyMetadata("Trend Chart"));

        public string XAxisText { get => (string)GetValue(XAxisTextProperty); set => SetValue(XAxisTextProperty, value); }
        public static readonly DependencyProperty XAxisTextProperty = DependencyProperty.Register(nameof(XAxisText), typeof(string), typeof(TrendChart), new PropertyMetadata("Day"));

        public string YAxisText { get => (string)GetValue(YAxisTextProperty); set => SetValue(YAxisTextProperty, value); }
        public static readonly DependencyProperty YAxisTextProperty = DependencyProperty.Register(nameof(YAxisText), typeof(string), typeof(TrendChart), new PropertyMetadata("sec"));

        // ===================== Drawing Engine =====================

        private void Redraw()
        {
            PlotCanvas.Children.Clear();

            if (Values == null) return;

            // Get actual drawing dimensions
            double fullW = PlotCanvas.ActualWidth;
            double fullH = PlotCanvas.ActualHeight;

            if (fullW <= PadLeft + PadRight || fullH <= PadTop + PadBottom) return;

            // Define the "Safe Area" for the line chart
            double graphW = fullW - PadLeft - PadRight;
            double graphH = fullH - PadTop - PadBottom;

            // Get Data (Last 7 Only)
            var fullList = Values.ToList();
            if (fullList.Count == 0) return;
            var list = fullList.TakeLast(7).ToList();
            int count = list.Count;

            // Calculate Min/Max with buffer
            double min = list.Min();
            double max = list.Max();
            double buffer = (max - min) * 0.1;
            if (buffer == 0) buffer = 1; // handle flat line case

            double axisMin = min - buffer;
            double axisMax = max + buffer;
            double range = axisMax - axisMin;

            // Generate Points mapped to the Safe Area
            var points = new List<Point>();
            double stepX = (count > 1) ? graphW / (count - 1) : graphW / 2;

            for (int i = 0; i < count; i++)
            {
                // X Calculation: PadLeft + step
                double x = PadLeft + (i * stepX);

                // Y Calculation: Map value to height, then flip (since 0 is top)
                double normalized = (list[i] - axisMin) / range;
                double y = PadTop + graphH - (normalized * graphH);

                points.Add(new Point(x, y));
            }

            // Draw Elements
            DrawGridAndLabels(graphW, graphH, axisMin, axisMax, count, stepX);
            if (points.Count > 1) DrawSmoothSpline(points);
            DrawPoints(points);
        }

        private void DrawGridAndLabels(double w, double h, double min, double max, int count, double stepX)
        {
            // 1. Draw Horizontal Grid Lines & Y-Axis Text
            int steps = 4;
            for (int i = 0; i <= steps; i++)
            {
                double yRatio = (double)i / steps;
                double yPos = PadTop + (h * (1 - yRatio)); // Flip for drawing from bottom up

                // Grid Line (Only inside graph area)
                var line = new Line
                {
                    X1 = PadLeft,
                    X2 = PadLeft + w,
                    Y1 = yPos,
                    Y2 = yPos,
                    Stroke = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 4 } // Dashed effect
                };
                PlotCanvas.Children.Add(line);

                // Y-Axis Label (Value)
                double val = min + (yRatio * (max - min));
                var text = new TextBlock
                {
                    Text = $"{val:0.0}s",
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    FontSize = 10,
                    Width = 35,
                    TextAlignment = TextAlignment.Right
                };
                // Position to the LEFT of the chart area
                Canvas.SetLeft(text, 0);
                Canvas.SetTop(text, yPos - 7);
                PlotCanvas.Children.Add(text);
            }

            // 2. Draw X-Axis Labels (Days)
            var today = DateTime.Now;
            for (int i = 0; i < count; i++)
            {
                int daysAgo = (count - 1) - i;
                string dayLabel = today.AddDays(-daysAgo).ToString("ddd");

                var text = new TextBlock
                {
                    Text = dayLabel,
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                    FontSize = 10,
                    Width = 40,
                    TextAlignment = TextAlignment.Center
                };

                // Position exactly under the data point
                double xPos = PadLeft + (i * stepX) - 20;
                Canvas.SetLeft(text, xPos);
                Canvas.SetTop(text, PadTop + h + 5); // Below graph area
                PlotCanvas.Children.Add(text);
            }
        }

        private void DrawSmoothSpline(List<Point> points)
        {
            var geom = new PathGeometry();
            var figure = new PathFigure { StartPoint = points[0] };

            for (int i = 1; i < points.Count; i++)
            {
                // Smooth curve using Bezier
                double midX = (points[i - 1].X + points[i].X) / 2;
                var p1 = new Point(midX, points[i - 1].Y);
                var p2 = new Point(midX, points[i].Y);

                var segment = new BezierSegment(p1, p2, points[i], true);
                figure.Segments.Add(segment);
            }

            geom.Figures.Add(figure);

            var path = new Path
            {
                Stroke = new SolidColorBrush(Color.FromRgb(0, 255, 150)),
                StrokeThickness = 2,
                Data = geom,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.LimeGreen,
                    BlurRadius = 8,
                    ShadowDepth = 0,
                    Opacity = 0.4
                }
            };

            PlotCanvas.Children.Add(path);
        }

        private void DrawPoints(List<Point> points)
        {
            foreach (var p in points)
            {
                var dot = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = Brushes.White,
                    Stroke = Brushes.LimeGreen,
                    StrokeThickness = 2
                };
                Canvas.SetLeft(dot, p.X - 4);
                Canvas.SetTop(dot, p.Y - 4);
                PlotCanvas.Children.Add(dot);
            }
        }

       /* private void PlotCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Redrawing logic for tooltip...
            // Must respect the same layout logic (PadLeft, GraphW)
            if (Values == null) return;
            var fullList = Values.ToList();
            if (fullList.Count == 0) return;
            var list = fullList.TakeLast(7).ToList();

            double w = PlotCanvas.ActualWidth - PadLeft - PadRight;
            if (w <= 0) return;

            var pos = e.GetPosition(PlotCanvas);

            // Check bounds inside graph area only
            if (pos.X < PadLeft || pos.X > PadLeft + w)
            {
                TooltipBox.Visibility = Visibility.Collapsed;
                return;
            }

            double relativeX = pos.X - PadLeft;
            int count = list.Count;
            double stepX = w / (count - 1);

            int index = (int)Math.Round(relativeX / stepX);
            index = Math.Max(0, Math.Min(index, count - 1));

            TooltipText.Text = $"{list[index]:0.00} s";
            TooltipBox.Visibility = Visibility.Visible;

            double tLeft = PadLeft + (index * stepX) + 10;
            double tTop = pos.Y - 30;

            // Keep tooltip inside canvas
            if (tLeft + TooltipBox.ActualWidth > PlotCanvas.ActualWidth)
                tLeft -= (TooltipBox.ActualWidth + 20);

            Canvas.SetLeft(TooltipBox, tLeft);
            Canvas.SetTop(TooltipBox, tTop);
        }*/
    }
}