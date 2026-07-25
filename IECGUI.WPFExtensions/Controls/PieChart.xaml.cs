using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IECGUI.Common.WPFExtensions.Controls
{
    public class PieSliceModel
    {
        public string Label { get; set; }
        public double Value { get; set; }
    }

    public partial class PieChart : UserControl
    {
        public PieChart()
        {
            InitializeComponent();

            Loaded += (_, __) => DrawPie();
            SizeChanged += (_, __) => DrawPie();
            PieCanvas.MouseMove += PieCanvas_MouseMove;
            PieCanvas.MouseLeave += (_, __) => Tooltip.Visibility = Visibility.Collapsed;

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                Items = new List<PieSliceModel>
                {
                    new PieSliceModel { Label="OK", Value=60 },
                    new PieSliceModel { Label="Tossed", Value=25 },
                    new PieSliceModel { Label="NG", Value=10 }
                };
                CenterText = "87.4%";
                DrawPie();
            }
        }


        // ================= DEPENDENCY PROPERTIES =================

        public IEnumerable<PieSliceModel> Items
        {
            get => (IEnumerable<PieSliceModel>)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items), typeof(IEnumerable<PieSliceModel>),
                typeof(PieChart), new PropertyMetadata(null, (d, e) => ((PieChart)d).DrawPie()));

        public string CenterText
        {
            get => (string)GetValue(CenterTextProperty);
            set => SetValue(CenterTextProperty, value);
        }
        public static readonly DependencyProperty CenterTextProperty =
            DependencyProperty.Register(nameof(CenterText), typeof(string),
                typeof(PieChart), new PropertyMetadata(""));



        // ================= PIE CHART DRAW ENGINE =================

        private readonly Brush[] sliceColors = new Brush[]
        {
            Brushes.LimeGreen,
            Brushes.Orange,
            Brushes.Red,
            Brushes.DeepSkyBlue
        };

        private List<(Path path, PieSliceModel slice)> sliceHitAreas = new();

        private void DrawPie()
        {
            PieCanvas.Children.Clear();
            sliceHitAreas.Clear();

            if (Items == null || !Items.Any()) return;

            double total = Items.Sum(x => x.Value);
            if (total == 0) return;

            double w = PieCanvas.ActualWidth;
            double h = PieCanvas.ActualHeight;
            double size = Math.Min(w, h);

            double radius = size / 2;
            double innerRadius = radius * 0.55; // donut thickness

            Point center = new Point(w / 2, h / 2);

            double angleStart = 0;
            int colorIndex = 0;

            foreach (var slice in Items)
            {
                double percentage = slice.Value / total;
                double sweep = percentage * 360;

                Path donutSlice = CreateDonutSlice(center, radius, innerRadius, angleStart, sweep);
                donutSlice.Fill = sliceColors[colorIndex % sliceColors.Length];

                PieCanvas.Children.Add(donutSlice);
                sliceHitAreas.Add((donutSlice, slice));

                // ---- LABEL + CONNECTOR ----
                DrawLabel(center, radius, angleStart, sweep, slice.Label, percentage);

                angleStart += sweep;
                colorIndex++;
            }
        }


        // ======= Create Donut Slice =======
        private Path CreateDonutSlice(Point center, double outerR, double innerR, double startAngle, double sweep)
        {
            GeometryGroup group = new();

            bool isLargeArc = sweep > 180;

            double startRad = startAngle * Math.PI / 180;
            double endRad = (startAngle + sweep) * Math.PI / 180;


            // Outer arc points
            Point outerStart = new(center.X + outerR * Math.Cos(startRad),
                                   center.Y + outerR * Math.Sin(startRad));

            Point outerEnd = new(center.X + outerR * Math.Cos(endRad),
                                 center.Y + outerR * Math.Sin(endRad));


            // Inner arc points
            Point innerEnd = new(center.X + innerR * Math.Cos(endRad),
                                 center.Y + innerR * Math.Sin(endRad));

            Point innerStart = new(center.X + innerR * Math.Cos(startRad),
                                   center.Y + innerR * Math.Sin(startRad));


            PathFigure fig = new();
            fig.StartPoint = outerStart;

            fig.Segments.Add(new ArcSegment(outerEnd, new Size(outerR, outerR), sweep, isLargeArc,
                                            SweepDirection.Clockwise, true));

            fig.Segments.Add(new LineSegment(innerEnd, true));
            fig.Segments.Add(new ArcSegment(innerStart, new Size(innerR, innerR), sweep, isLargeArc,
                                            SweepDirection.Counterclockwise, true));

            fig.Segments.Add(new LineSegment(outerStart, true));

            PathGeometry geom = new();
            geom.Figures.Add(fig);

            return new Path { Data = geom };
        }



        // ======= LABEL DRAWING =======
        private void DrawLabel(Point center, double radius, double startAngle, double sweep,
                               string label, double percent)
        {
            double midAngle = startAngle + sweep / 2;
            double rad = midAngle * Math.PI / 180;

            double lineStartR = radius * 0.75;
            double lineEndR = radius * 0.95;

            Point p1 = new(center.X + lineStartR * Math.Cos(rad),
                           center.Y + lineStartR * Math.Sin(rad));

            Point p2 = new(center.X + lineEndR * Math.Cos(rad),
                           center.Y + lineEndR * Math.Sin(rad));

            PieCanvas.Children.Add(new Line
            {
                X1 = p1.X,
                Y1 = p1.Y,
                X2 = p2.X,
                Y2 = p2.Y,
                Stroke = Brushes.White,
                StrokeThickness = 1
            });

            // Text label
            string text = $"{label}  {(percent * 100):0.#}%";

            TextBlock tb = new()
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };

            Canvas.SetLeft(tb, p2.X + 5);
            Canvas.SetTop(tb, p2.Y - 10);

            PieCanvas.Children.Add(tb);
        }



        // ================= TOOLTIP =================
        private void PieCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Point pos = e.GetPosition(PieCanvas);

            foreach (var hit in sliceHitAreas)
            {
                if (hit.path.IsMouseDirectlyOver)
                {
                    double total = Items.Sum(x => x.Value);
                    double pct = (hit.slice.Value / total) * 100;

                    TooltipText.Text = $"{hit.slice.Label}\n{hit.slice.Value} ({pct:0.#}%)";

                    Tooltip.Visibility = Visibility.Visible;

                    Canvas.SetLeft(Tooltip, pos.X + 10);
                    Canvas.SetTop(Tooltip, pos.Y - 20);

                    return;
                }
            }

            Tooltip.Visibility = Visibility.Collapsed;
        }
    }
}
