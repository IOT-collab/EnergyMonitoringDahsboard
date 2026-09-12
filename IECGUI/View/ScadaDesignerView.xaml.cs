using IECGUI.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IECGUI.View;

public partial class ScadaDesignerView : UserControl
{
    private ScadaWidgetViewModel? _dragWidget;
    private Point _dragOffset;

    public ScadaDesignerView()
    {
        InitializeComponent();
    }

    private void WidgetMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not ScadaWidgetViewModel widget)
            return;

        if (DataContext is ScadaDesignerViewModel viewModel)
            viewModel.SelectedWidget = widget;

        _dragWidget = widget;
        var point = e.GetPosition(DesignCanvas);
        _dragOffset = new Point(point.X - widget.X, point.Y - widget.Y);
        element.CaptureMouse();
        e.Handled = true;
    }

    private void WidgetMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragWidget == null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var point = e.GetPosition(DesignCanvas);
        _dragWidget.MoveTo(point.X - _dragOffset.X, point.Y - _dragOffset.Y);
    }

    private void WidgetMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is UIElement element)
            element.ReleaseMouseCapture();
        _dragWidget = null;
    }
}
