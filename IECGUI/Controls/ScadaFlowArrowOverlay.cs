using System;
using System.Windows;
using System.Windows.Media;

namespace IECGUI.Controls;

/// <summary>
/// Renders one compact triangular arrowhead travelling along a line.
/// </summary>
public sealed class ScadaFlowArrowOverlay : FrameworkElement
{
    public static readonly DependencyProperty IsRunningProperty =
        DependencyProperty.Register(nameof(IsRunning), typeof(bool), typeof(ScadaFlowArrowOverlay),
            new FrameworkPropertyMetadata(false, OnAnimationPropertyChanged));

    public static readonly DependencyProperty ArrowDirectionProperty =
        DependencyProperty.Register(nameof(ArrowDirection), typeof(string), typeof(ScadaFlowArrowOverlay),
            new FrameworkPropertyMetadata("Forward", OnAnimationPropertyChanged));

    public static readonly DependencyProperty ArrowColorProperty =
        DependencyProperty.Register(nameof(ArrowColor), typeof(string), typeof(ScadaFlowArrowOverlay),
            new FrameworkPropertyMetadata("#E6F8FF", OnAnimationPropertyChanged));

    public static readonly DependencyProperty LineThicknessProperty =
        DependencyProperty.Register(nameof(LineThickness), typeof(double), typeof(ScadaFlowArrowOverlay),
            new FrameworkPropertyMetadata(2d, OnAnimationPropertyChanged));

    private double _phase;
    private DateTime _lastFrame;
    private bool _rendering;

    public bool IsRunning { get => (bool)GetValue(IsRunningProperty); set => SetValue(IsRunningProperty, value); }
    public string ArrowDirection { get => (string)GetValue(ArrowDirectionProperty); set => SetValue(ArrowDirectionProperty, value); }
    public string ArrowColor { get => (string)GetValue(ArrowColorProperty); set => SetValue(ArrowColorProperty, value); }
    public double LineThickness { get => (double)GetValue(LineThicknessProperty); set => SetValue(LineThicknessProperty, value); }

    public ScadaFlowArrowOverlay()
    {
        IsHitTestVisible = false;
        Loaded += (_, _) => UpdateAnimationState();
        Unloaded += (_, _) => StopAnimation();
    }

    private static void OnAnimationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var overlay = (ScadaFlowArrowOverlay)d;
        overlay.UpdateAnimationState();
        overlay.InvalidateVisual();
    }

    private void UpdateAnimationState()
    {
        if (IsRunning && IsLoaded)
        {
            if (_rendering)
                return;

            _lastFrame = DateTime.UtcNow;
            _rendering = true;
            CompositionTarget.Rendering += OnRendering;
            return;
        }

        StopAnimation();
    }

    private void StopAnimation()
    {
        if (_rendering)
            CompositionTarget.Rendering -= OnRendering;

        _rendering = false;
        _phase = 0;
        InvalidateVisual();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!IsRunning)
        {
            StopAnimation();
            return;
        }

        var now = DateTime.UtcNow;
        var elapsed = Math.Clamp((now - _lastFrame).TotalSeconds, 0d, 0.1d);
        _lastFrame = now;
        _phase = (_phase + elapsed / 1.4d) % 1d;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (!IsRunning || ActualWidth <= 1 || ActualHeight <= 1)
            return;

        var width = ActualWidth;
        var height = ActualHeight;
        var isHorizontal = width >= height && height <= Math.Max(2d, width * 0.15d);
        var isVertical = height > width && width <= Math.Max(2d, height * 0.15d);
        var axis = isHorizontal ? new Vector(width, 0) : isVertical ? new Vector(0, -height) : new Vector(width, -height);
        var lineLength = axis.Length;
        if (lineLength <= 1)
            return;

        axis.Normalize();
        var reverse = string.Equals(ArrowDirection, "Reverse", StringComparison.OrdinalIgnoreCase);
        var direction = reverse ? -axis : axis;
        var normal = new Vector(-direction.Y, direction.X);
        var start = isHorizontal ? new Point(0, height / 2d) : isVertical ? new Point(width / 2d, height) : new Point(0, height);
        // Keep one small arrow fully inside the line bounds. This avoids overlapping
        // arrowheads on short conductors while retaining a visible travelling marker.
        var arrowLength = Math.Min(Math.Clamp(lineLength * 0.22d, 5d, 22d), lineLength * 0.8d);
        var arrowWidth = Math.Min(Math.Clamp(Math.Max(5d, LineThickness * 2.2d), 5d, 14d), Math.Max(3d, lineLength * 0.5d));
        if (arrowLength <= 0.5d || arrowWidth <= 0.5d)
            return;
        var halfWidth = arrowWidth / 2d;
        var brush = CreateBrush(ArrowColor);
        var outline = new Pen(brush, Math.Max(1d, LineThickness * 0.45d));
        outline.Freeze();

        var span = arrowLength / lineLength;
        var lineProgress = reverse
            ? 1d - _phase * (1d - span)
            : span + _phase * (1d - span);
        var tip = start + axis * (lineLength * lineProgress);
        var basePoint = tip - direction * arrowLength;
        var left = basePoint + normal * halfWidth;
        var right = basePoint - normal * halfWidth;

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(tip, isFilled: true, isClosed: true);
            context.LineTo(left, isStroked: true, isSmoothJoin: true);
            context.LineTo(right, isStroked: true, isSmoothJoin: true);
        }

        geometry.Freeze();
        drawingContext.DrawGeometry(brush, outline, geometry);

    }

    private static Brush CreateBrush(string? color)
    {
        try
        {
            var brush = (Brush)new BrushConverter().ConvertFromString(color ?? "#E6F8FF")!;
            brush.Freeze();
            return brush;
        }
        catch
        {
            return Brushes.White;
        }
    }
}
