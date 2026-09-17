using IEC.Shared.Models;
using IECGUI.ViewModel;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Shapes;

namespace IECGUI.View;

public partial class ScadaDesignerView : UserControl
{
    private ScadaWidgetViewModel? _dragWidget;
    private Point _dragOffset;
    private Point _lastDragPoint;
    private bool _resizing;
    private Button? _paletteButton;
    private Point _paletteStartPoint;
    private bool _paletteDragging;
    private bool _suppressPaletteClick;
    private bool _isSelecting;
    private bool _selectionAdditive;
    private Point _selectionStart;
    private ScadaSymbolLibraryItem? _symbolDragItem;
    private Point _symbolStartPoint;
    private bool _symbolDragging;

    public ScadaDesignerView()
    {
        InitializeComponent();
        Focusable = true;
    }

    private void DesignerLoaded(object sender, RoutedEventArgs e)
    {
        Focus();
    }
    private ScadaDesignerViewModel? ViewModel => DataContext as ScadaDesignerViewModel;

    private void DesignerPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.OriginalSource is TextBox) return;
        if (e.Key == Key.Delete)
        {
            ViewModel?.DeleteSelected();
            e.Handled = true;
            return;
        }
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
        {
            ViewModel?.CopySelected();
            e.Handled = true;
            return;
        }
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V)
        {
            ViewModel?.PasteSelected();
            e.Handled = true;
        }
    }

    private void WidgetMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_resizing || sender is not FrameworkElement element || element.DataContext is not ScadaWidgetViewModel widget) return;
        ViewModel?.SelectWidget(widget, Keyboard.Modifiers.HasFlag(ModifierKeys.Control));
        _dragWidget = widget;
        var point = e.GetPosition(DesignCanvas);
        _dragOffset = new Point(point.X - widget.X, point.Y - widget.Y);
        _lastDragPoint = point;
        Focus();
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
        if (e.OriginalSource != DesignCanvas) return;
        Focus();
        _isSelecting = true;
        _selectionAdditive = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        _selectionStart = e.GetPosition(DesignCanvas);
        if (!_selectionAdditive) ViewModel?.ClearSelection();
        UpdateSelectionMarquee(_selectionStart);
        DesignCanvas.CaptureMouse();
        e.Handled = true;
    }
    private void CanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isSelecting || e.LeftButton != MouseButtonState.Pressed) return;
        UpdateSelectionMarquee(e.GetPosition(DesignCanvas));
        e.Handled = true;
    }
    private void CanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting) return;
        var current = e.GetPosition(DesignCanvas);
        UpdateSelectionMarquee(current);
        DesignCanvas.ReleaseMouseCapture();
        _isSelecting = false;
        SelectionMarquee.Visibility = Visibility.Collapsed;
        var x = Math.Min(_selectionStart.X, current.X);
        var y = Math.Min(_selectionStart.Y, current.Y);
        var width = Math.Abs(current.X - _selectionStart.X);
        var height = Math.Abs(current.Y - _selectionStart.Y);
        if (width >= 4 && height >= 4)
            ViewModel?.SelectWidgetsInRect(new Rect(x, y, width, height), _selectionAdditive);
        else if (!_selectionAdditive)
            ViewModel?.ClearSelection();
        e.Handled = true;
    }
    private void UpdateSelectionMarquee(Point current)
    {
        var x = Math.Min(_selectionStart.X, current.X);
        var y = Math.Min(_selectionStart.Y, current.Y);
        Canvas.SetLeft(SelectionMarquee, x);
        Canvas.SetTop(SelectionMarquee, y);
        SelectionMarquee.Width = Math.Max(1, Math.Abs(current.X - _selectionStart.X));
        SelectionMarquee.Height = Math.Max(1, Math.Abs(current.Y - _selectionStart.Y));
        SelectionMarquee.Visibility = Visibility.Visible;
    }
    // A click adds one object; a drag starts only after the pointer crosses the system drag threshold.
    // Click and drag are deliberately separate so a completed drag cannot also add a click object.
    private void PaletteMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string) return;
        _paletteButton = button;
        _paletteStartPoint = e.GetPosition(button);
        _paletteDragging = false;
        _suppressPaletteClick = false;
        button.CaptureMouse();
        // Leave the preview event unhandled so Button can raise Click for a normal press.
    }

    private void PaletteMouseMove(object sender, MouseEventArgs e)
    {
        if (_paletteDragging || sender != _paletteButton || e.LeftButton != MouseButtonState.Pressed || _paletteButton?.Tag is not string tag) return;
        var current = e.GetPosition(_paletteButton);
        var delta = current - _paletteStartPoint;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        if (!Enum.TryParse<ScadaWidgetType>(tag, true, out var type)) return;

        // Set this before DoDragDrop. WPF may raise Click after a drag completes; that click must be ignored.
        _paletteDragging = true;
        _suppressPaletteClick = true;
        DragDrop.DoDragDrop(_paletteButton, type.ToString(), DragDropEffects.Copy);
        e.Handled = true;
    }

    private void PaletteMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Button button || button != _paletteButton) return;
        button.ReleaseMouseCapture();
        _paletteButton = null;
        _paletteDragging = false;
    }

    private void PaletteClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag || !Enum.TryParse<ScadaWidgetType>(tag, true, out var type)) return;
        if (_suppressPaletteClick)
        {
            _suppressPaletteClick = false;
            return;
        }
        ViewModel?.AddWidget(type);
    }
    private void SymbolMouseDown(object sender, MouseButtonEventArgs e)
    {
        var item = FindVisualParent<ListBoxItem>(e.OriginalSource as DependencyObject)?.DataContext as ScadaSymbolLibraryItem;
        if (item == null) return;
        _symbolDragItem = item;
        _symbolStartPoint = e.GetPosition(SymbolList);
        _symbolDragging = false;
    }

    private void SymbolMouseMove(object sender, MouseEventArgs e)
    {
        if (_symbolDragItem == null || _symbolDragging || e.LeftButton != MouseButtonState.Pressed) return;
        var current = e.GetPosition(SymbolList);
        var delta = current - _symbolStartPoint;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        _symbolDragging = true;
        DragDrop.DoDragDrop(SymbolList, $"symbol:{_symbolDragItem.Id}", DragDropEffects.Copy);
    }

    private void SymbolMouseUp(object sender, MouseButtonEventArgs e)
    {
        _symbolDragItem = null;
        _symbolDragging = false;
    }

    private void BrowseSymbolClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*",
            Multiselect = false
        };
        if (dialog.ShowDialog() == true) ViewModel?.AddCustomSymbol(dialog.FileName);
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T match) return match;
            child = System.Windows.Media.VisualTreeHelper.GetParent(child);
        }
        return null;
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
        var point = e.GetPosition(DesignCanvas);
        if (tag?.StartsWith("symbol:", StringComparison.OrdinalIgnoreCase) == true) ViewModel.AddSymbolAt(tag.Substring("symbol:".Length), point);
        else if (Enum.TryParse<ScadaWidgetType>(tag, true, out var type)) ViewModel.AddWidgetAt(type, point);
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
