using IEC.Shared.Models;
using IECGUI.ViewModel;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace IECGUI.View;

public partial class ScadaDesignerView : UserControl
{
    private ScadaWidgetViewModel? _dragWidget;
    private Point _dragOffset;
    private Point _lastDragPoint;
    private bool _resizing;
    private bool _suppressPaletteClick;

    public ScadaDesignerView() => InitializeComponent();
    private ScadaDesignerViewModel? ViewModel => DataContext as ScadaDesignerViewModel;

    private void WidgetMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_resizing || sender is not FrameworkElement element || element.DataContext is not ScadaWidgetViewModel widget) return;
        ViewModel?.SelectWidget(widget, Keyboard.Modifiers.HasFlag(ModifierKeys.Control));
        _dragWidget = widget;
        var point = e.GetPosition(DesignCanvas);
        _dragOffset = new Point(point.X - widget.X, point.Y - widget.Y);
        _lastDragPoint = point;
        element.CaptureMouse();
        e.Handled = true;
    }

    private void WidgetMouseMove(object sender, MouseEventArgs e)
    {
        if (_resizing || _dragWidget == null || e.LeftButton != MouseButtonState.Pressed) return;
        var point = e.GetPosition(DesignCanvas);
        var dx = point.X - _lastDragPoint.X;
        var dy = point.Y - _lastDragPoint.Y;
        var moving = _dragWidget.GroupId != null
            ? ViewModel?.Widgets.Where(x => string.Equals(x.GroupId, _dragWidget.GroupId, StringComparison.OrdinalIgnoreCase))
            : ViewModel?.SelectedWidgets.Count > 1 ? ViewModel.SelectedWidgets : new[] { _dragWidget };
        if (moving != null)
            foreach (var widget in moving) widget.MoveTo(widget.X + dx, widget.Y + dy);
        _lastDragPoint = point;
    }

    private void WidgetMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is UIElement element) element.ReleaseMouseCapture();
        _dragWidget = null;
    }

    private void CanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == DesignCanvas) ViewModel?.ClearSelection();
    }

    private void PaletteClick(object sender, RoutedEventArgs e)
    {
        if (_suppressPaletteClick) { _suppressPaletteClick = false; return; }
        if (sender is Button button && button.Tag is string tag && Enum.TryParse<ScadaWidgetType>(tag, true, out var type))
            ViewModel?.AddWidget(type);
    }

    private void PaletteMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || sender is not Button button || button.Tag is not string tag) return;
        if (!Enum.TryParse<ScadaWidgetType>(tag, true, out var type)) return;
        _suppressPaletteClick = true;
        DragDrop.DoDragDrop(button, type.ToString(), DragDropEffects.Copy);
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, new Action(() => _suppressPaletteClick = false));
        e.Handled = true;
    }

    private void CanvasDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.StringFormat) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void CanvasDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.StringFormat) || ViewModel == null) return;
        var tag = e.Data.GetData(DataFormats.StringFormat) as string;
        if (Enum.TryParse<ScadaWidgetType>(tag, true, out var type)) ViewModel.AddWidgetAt(type, e.GetPosition(DesignCanvas));
        e.Handled = true;
    }

    private void ResizeHandleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Thumb thumb || thumb.DataContext is not ScadaWidgetViewModel widget) return;
        _resizing = true;
        ViewModel?.SelectWidget(widget, false);
        e.Handled = false;
    }

    private void ResizeHandleDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb thumb || thumb.DataContext is not ScadaWidgetViewModel widget || thumb.Tag is not string edge) return;
        var dx = e.HorizontalChange;
        var dy = e.VerticalChange;
        if (edge.Contains('W'))
        {
            var width = Math.Max(1, widget.Width - dx);
            widget.X += widget.Width - width;
            widget.Width = width;
        }
        else if (edge.Contains('E')) widget.Width = Math.Max(1, widget.Width + dx);
        if (edge.Contains('N'))
        {
            var height = Math.Max(1, widget.Height - dy);
            widget.Y += widget.Height - height;
            widget.Height = height;
        }
        else if (edge.Contains('S')) widget.Height = Math.Max(1, widget.Height + dy);
        e.Handled = true;
    }

    private void ResizeHandleDragCompleted(object sender, DragCompletedEventArgs e) => _resizing = false;
}
