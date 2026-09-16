using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace IECGUI.ViewModel;

public sealed class ScadaDesignerViewModel : BaseViewModel
{
    private readonly ConfigurationManagerService _configuration;
    private readonly DeviceRuntimeService _runtime;
    private readonly ScadaLayoutService _layouts;
    private readonly INavigationService _navigation;
    private ScadaPageConfig? _selectedPage;
    private ScadaWidgetViewModel? _selectedWidget;
    private string _status = "Create a mimic page by adding objects to the canvas.";
    private string _colorTarget = "Foreground";

    public ObservableCollection<ScadaPageConfig> Pages { get; } = new();
    public ObservableCollection<ScadaWidgetViewModel> Widgets { get; } = new();
    public ObservableCollection<ScadaWidgetViewModel> SelectedWidgets { get; } = new();
    public ObservableCollection<string> AvailableDevices { get; } = new();
    public ObservableCollection<string> AvailableParameters { get; } = new();
    public ObservableCollection<string> ColorTargets { get; } = new() { "Foreground", "Background", "ON foreground", "ON background", "OFF foreground", "OFF background" };

    public string ColorTarget { get => _colorTarget; set => SetProperty(ref _colorTarget, value); }
    public bool HasMultiSelection => SelectedWidgets.Count > 1;
    public ScadaPageConfig? SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (!SetProperty(ref _selectedPage, value) || value == null) return;
            LoadSelectedPage();
        }
    }

    public ScadaWidgetViewModel? SelectedWidget
    {
        get => _selectedWidget;
        set
        {
            if (!SetProperty(ref _selectedWidget, value)) return;
            RefreshParameters();
        }
    }

    public double CanvasWidth => SelectedPage?.CanvasWidth ?? 1500;
    public double CanvasHeight => SelectedPage?.CanvasHeight ?? 800;
    public string Status { get => _status; private set => SetProperty(ref _status, value); }

    public ICommand NewPageCommand { get; }
    public ICommand DeletePageCommand { get; }
    public ICommand AddLabelCommand { get; }
    public ICommand AddValueCommand { get; }
    public ICommand AddLedCommand { get; }
    public ICommand AddButtonCommand { get; }
    public ICommand AddLineCommand { get; }
    public ICommand AddRectangleCommand { get; }
    public ICommand AddCircleCommand { get; }
    public ICommand DeleteWidgetCommand { get; }
    public ICommand GroupCommand { get; }
    public ICommand UngroupCommand { get; }
    public ICommand BringForwardCommand { get; }
    public ICommand SendBackwardCommand { get; }
    public ICommand BringToFrontCommand { get; }
    public ICommand SendToBackCommand { get; }
    public ICommand AdjustCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand OpenRuntimeCommand { get; }
    public ICommand ApplyColorCommand { get; }

    public ScadaDesignerViewModel(
        ConfigurationManagerService configuration,
        DeviceRuntimeService runtime,
        ScadaLayoutService layouts,
        INavigationService navigation,
        IDialogService dialog)
    {
        _configuration = configuration;
        _runtime = runtime;
        _layouts = layouts;
        _navigation = navigation;

        foreach (var meter in (_configuration.Configuration.Meters ?? new())
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.MeterName))
            .OrderBy(x => x.MeterName))
            AvailableDevices.Add(meter.MeterName);

        NewPageCommand = new RelayCommand(NewPage);
        DeletePageCommand = new RelayCommand(DeletePage, () => Pages.Count > 1);
        AddLabelCommand = new RelayCommand(() => AddWidget(ScadaWidgetType.Label));
        AddValueCommand = new RelayCommand(() => AddWidget(ScadaWidgetType.Value));
        AddLedCommand = new RelayCommand(() => AddWidget(ScadaWidgetType.Led));
        AddButtonCommand = new RelayCommand(() => AddWidget(ScadaWidgetType.Button));
        AddLineCommand = new RelayCommand(() => AddWidget(ScadaWidgetType.Line));
        AddRectangleCommand = new RelayCommand(() => AddWidget(ScadaWidgetType.Rectangle));
        AddCircleCommand = new RelayCommand(() => AddWidget(ScadaWidgetType.Circle));
        DeleteWidgetCommand = new RelayCommand(DeleteSelectedWidget);
        GroupCommand = new RelayCommand(GroupSelected);
        UngroupCommand = new RelayCommand(UngroupSelected);
        BringForwardCommand = new RelayCommand(() => MoveSelected(1));
        SendBackwardCommand = new RelayCommand(() => MoveSelected(-1));
        BringToFrontCommand = new RelayCommand(() => MoveSelectedToEdge(true));
        SendToBackCommand = new RelayCommand(() => MoveSelectedToEdge(false));
        AdjustCommand = new RelayCommand<string>(AdjustSelected);
        SaveCommand = new RelayCommand(Save);
        BackCommand = new RelayCommand(() => _navigation.NavigateTo<HomePageViewModel>());
        OpenRuntimeCommand = new RelayCommand(() => _navigation.NavigateTo<ScadaRuntimeViewModel>());
        ApplyColorCommand = new RelayCommand<string>(ApplyColor);

        foreach (var page in _layouts.LoadPages()) Pages.Add(page);
        SelectedPage = Pages.FirstOrDefault();
        _layouts.LayoutSaved += OnLayoutSaved;
        _runtime.SnapshotUpdated += OnSnapshotUpdated;
        RefreshLiveValues();
    }

    private void OnLayoutSaved() => Status = $"Saved {Pages.Count} page(s) to the project configuration.";

    private void LoadSelectedPage()
    {
        Widgets.Clear();
        SelectedWidgets.Clear();
        if (SelectedPage == null) return;
        foreach (var widget in SelectedPage.Widgets ?? new()) Widgets.Add(new ScadaWidgetViewModel(widget));
        SelectWidget(Widgets.FirstOrDefault(), false);
        OnPropertyChanged(nameof(CanvasWidth));
        OnPropertyChanged(nameof(CanvasHeight));
        RefreshLiveValues();
    }

    private void NewPage()
    {
        var page = new ScadaPageConfig { PageName = $"Page {Pages.Count + 1}" };
        Pages.Add(page);
        SelectedPage = page;
        Status = $"Created {page.PageName}. Enter a page name and press SAVE.";
    }

    private void DeletePage()
    {
        if (SelectedPage == null || Pages.Count <= 1) return;
        var name = SelectedPage.PageName;
        Pages.Remove(SelectedPage);
        SelectedPage = Pages.FirstOrDefault();
        Status = $"Deleted {name}. Press SAVE to persist the change.";
    }

    public void AddWidget(ScadaWidgetType type) => AddWidgetAt(type, null);

    public void AddWidgetAt(ScadaWidgetType type, Point? location)
    {
        if (SelectedPage == null) return;
        var index = Widgets.Count;
        var config = new ScadaWidgetConfig
        {
            Type = type,
            Caption = type switch
            {
                ScadaWidgetType.Value => "Live value",
                ScadaWidgetType.Led => "Status",
                ScadaWidgetType.Button => "WRITE",
                ScadaWidgetType.Line => string.Empty,
                ScadaWidgetType.Rectangle => string.Empty,
                ScadaWidgetType.Circle => string.Empty,
                _ => "Label"
            },
            OnCaption = type == ScadaWidgetType.Button ? "ON" : string.Empty,
            OffCaption = type == ScadaWidgetType.Button ? "OFF" : string.Empty,
            X = Math.Max(0, location?.X ?? (80 + (index % 5) * 250)),
            Y = Math.Max(0, location?.Y ?? (70 + (index / 5) * 105)),
            Width = type == ScadaWidgetType.Line ? 220 : 210,
            Height = type == ScadaWidgetType.Line ? 8 : type == ScadaWidgetType.Circle ? 100 : 58,
            DeviceName = AvailableDevices.FirstOrDefault(),
            ParameterName = AvailableParameters.FirstOrDefault(),
            DynamicStateColors = type is ScadaWidgetType.Led or ScadaWidgetType.Button or ScadaWidgetType.Line
        };
        var item = new ScadaWidgetViewModel(config);
        Widgets.Add(item);
        SelectWidget(item, false);
        Status = $"Added {type}. Drag it on the canvas or configure its properties on the right.";
    }

    public void SelectWidget(ScadaWidgetViewModel? widget, bool additive)
    {
        if (!additive)
        {
            foreach (var selected in SelectedWidgets) selected.IsSelected = false;
            SelectedWidgets.Clear();
        }
        if (widget != null && !SelectedWidgets.Contains(widget))
        {
            SelectedWidgets.Add(widget);
            widget.IsSelected = true;
        }
        SelectedWidget = widget;
        OnPropertyChanged(nameof(HasMultiSelection));
    }

    public void ClearSelection()
    {
        SelectWidget(null, false);
    }

    private void DeleteSelectedWidget()
    {
        if (SelectedPage == null || SelectedWidgets.Count == 0) return;
        var removed = SelectedWidgets.ToList();
        foreach (var widget in removed) Widgets.Remove(widget);
        SelectedWidgets.Clear();
        SelectedWidget = Widgets.FirstOrDefault();
        Status = "Object(s) removed. Press SAVE to persist the change.";
    }

    private void GroupSelected()
    {
        if (SelectedWidgets.Count < 2)
        {
            Status = "Select at least two objects with Ctrl-click before grouping.";
            return;
        }
        var groupId = SelectedWidgets.Select(x => x.GroupId).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? Guid.NewGuid().ToString("N");
        foreach (var widget in SelectedWidgets) widget.GroupId = groupId;
        Status = "Objects grouped. Use Ungroup to release them.";
    }

    private void UngroupSelected()
    {
        var groups = SelectedWidgets.Where(x => !string.IsNullOrWhiteSpace(x.GroupId)).Select(x => x.GroupId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (groups.Count == 0) { Status = "Select grouped objects first."; return; }
        foreach (var widget in Widgets.Where(x => x.GroupId != null && groups.Contains(x.GroupId)).ToList()) widget.GroupId = null;
        Status = "Objects ungrouped.";
    }

    private void MoveSelected(int delta)
    {
        if (SelectedWidgets.Count == 0) return;
        var ordered = SelectedWidgets.OrderBy(x => Widgets.IndexOf(x)).ToList();
        if (delta > 0) ordered.Reverse();
        foreach (var widget in ordered)
        {
            var index = Widgets.IndexOf(widget);
            var target = Math.Clamp(index + delta, 0, Widgets.Count - 1);
            if (target != index) Widgets.Move(index, target);
        }
        Status = delta > 0 ? "Moved object(s) forward." : "Moved object(s) backward.";
    }

    private void MoveSelectedToEdge(bool front)
    {
        if (SelectedWidgets.Count == 0) return;
        var ordered = SelectedWidgets.OrderBy(x => Widgets.IndexOf(x)).ToList();
        if (front)
        {
            foreach (var widget in ordered) { Widgets.Remove(widget); Widgets.Add(widget); }
        }
        else
        {
            ordered.Reverse();
            foreach (var widget in ordered) { Widgets.Remove(widget); Widgets.Insert(0, widget); }
        }
        Status = front ? "Moved object(s) to front." : "Sent object(s) to back.";
    }

    private void AdjustSelected(string? adjustment)
    {
        if (SelectedWidget == null || string.IsNullOrWhiteSpace(adjustment)) return;
        const double positionStep = 1;
        const double sizeStep = 1;
        const double rotationStep = 1;
        switch (adjustment)
        {
            case "x-": SelectedWidget.X -= positionStep; break;
            case "x+": SelectedWidget.X += positionStep; break;
            case "y-": SelectedWidget.Y -= positionStep; break;
            case "y+": SelectedWidget.Y += positionStep; break;
            case "w-": SelectedWidget.Width -= sizeStep; break;
            case "w+": SelectedWidget.Width += sizeStep; break;
            case "h-": SelectedWidget.Height -= sizeStep; break;
            case "h+": SelectedWidget.Height += sizeStep; break;
            case "r-": SelectedWidget.Rotation -= rotationStep; break;
            case "r+": SelectedWidget.Rotation += rotationStep; break;
        }
    }

    private void Save()
    {
        if (SelectedPage == null) return;
        foreach (var page in Pages)
            page.Widgets = page == SelectedPage ? Widgets.Select(x => x.Model).ToList() : page.Widgets ?? new();
        if (_layouts.SavePages(Pages)) Status = $"Saved {Pages.Count} page(s) to the project configuration.";
        else Status = "Unable to save the SCADA layout.";
    }

    private void RefreshParameters()
    {
        AvailableParameters.Clear();
        var device = _configuration.Configuration.Meters?.FirstOrDefault(x => string.Equals(x.MeterName, SelectedWidget?.DeviceName, StringComparison.OrdinalIgnoreCase));
        foreach (var register in device?.Registers?.Where(x => x != null && x.IsEnabled) ?? Enumerable.Empty<RegisterConfig>())
        {
            var name = string.IsNullOrWhiteSpace(register.ParameterName) ? register.RegisterAddress.ToString(CultureInfo.InvariantCulture) : register.ParameterName;
            if (!AvailableParameters.Contains(name, StringComparer.OrdinalIgnoreCase)) AvailableParameters.Add(name);
        }
    }

    private void ApplyColor(string color)
    {
        if (SelectedWidget == null || string.IsNullOrWhiteSpace(color)) return;
        SelectedWidget.ApplyColor(ColorTarget, color);
    }

    private void OnSnapshotUpdated() => RunOnUi(RefreshLiveValues);
    private void RefreshLiveValues()
    {
        var snapshot = _runtime.GetSnapshot();
        foreach (var widget in Widgets)
        {
            if (string.IsNullOrWhiteSpace(widget.DeviceName) || string.IsNullOrWhiteSpace(widget.ParameterName) || !snapshot.TryGetValue(widget.DeviceName, out var reading) || reading?.Values == null)
            { widget.LiveValue = "--"; continue; }
            var match = reading.Values.FirstOrDefault(x => string.Equals(x.Key, widget.ParameterName, StringComparison.OrdinalIgnoreCase));
            widget.LiveValue = match.Value == null ? "--" : FormatValue(match.Value);
        }
    }

    private static string FormatValue(object value)
    {
        if (value is bool b) return b ? "ON" : "OFF";
        if (value is IFormattable f) return f.ToString(null, CultureInfo.InvariantCulture);
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "--";
    }

    private static void RunOnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess()) action(); else dispatcher.BeginInvoke(action);
    }
}

public sealed class ScadaWidgetViewModel : ObservableObjectVM
{
    public ScadaWidgetConfig Model { get; }
    private string _liveValue = "--";
    private bool _isSelected;
    public ScadaWidgetViewModel(ScadaWidgetConfig model) => Model = model;
    public string Id => Model.Id;
    public ScadaWidgetType Type { get => Model.Type; set { if (Model.Type == value) return; Model.Type = value; OnPropertyChanged(); RaiseVisualProperties(); } }
    public string Caption { get => Model.Caption; set { if (Model.Caption == value) return; Model.Caption = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayValue)); OnPropertyChanged(nameof(EffectiveCaption)); } }
    public string OnCaption { get => Model.OnCaption; set { if (Model.OnCaption == value) return; Model.OnCaption = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveCaption)); } }
    public string OffCaption { get => Model.OffCaption; set { if (Model.OffCaption == value) return; Model.OffCaption = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveCaption)); } }
    public double X { get => Model.X; set { if (Math.Abs(Model.X - value) < 0.01) return; Model.X = Math.Max(0, value); OnPropertyChanged(); } }
    public double Y { get => Model.Y; set { if (Math.Abs(Model.Y - value) < 0.01) return; Model.Y = Math.Max(0, value); OnPropertyChanged(); } }
    public double Width { get => Model.Width; set { var v = Math.Max(1, value); if (Math.Abs(Model.Width - v) < 0.01) return; Model.Width = v; OnPropertyChanged(); } }
    public double Height { get => Model.Height; set { var v = Math.Max(1, value); if (Math.Abs(Model.Height - v) < 0.01) return; Model.Height = v; OnPropertyChanged(); } }
    public double Rotation { get => Model.Rotation; set { if (Math.Abs(Model.Rotation - value) < 0.01) return; Model.Rotation = value; OnPropertyChanged(); } }
    public string? GroupId { get => Model.GroupId; set { if (Model.GroupId == value) return; Model.GroupId = value; OnPropertyChanged(); } }
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public string DeviceName { get => Model.DeviceName ?? string.Empty; set { if (Model.DeviceName == value) return; Model.DeviceName = value; OnPropertyChanged(); } }
    public string ParameterName { get => Model.ParameterName ?? string.Empty; set { if (Model.ParameterName == value) return; Model.ParameterName = value; OnPropertyChanged(); } }
    public string Unit { get => Model.Unit; set { if (Model.Unit == value) return; Model.Unit = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayValue)); } }
    public string Foreground { get => Model.Foreground; set { if (Model.Foreground == value) return; Model.Foreground = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveForeground)); } }
    public string Background { get => Model.Background; set { if (Model.Background == value) return; Model.Background = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveBackground)); } }
    public bool DynamicStateColors { get => Model.DynamicStateColors; set { if (Model.DynamicStateColors == value) return; Model.DynamicStateColors = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveForeground)); OnPropertyChanged(nameof(EffectiveBackground)); } }
    public string OnForeground { get => Model.OnForeground; set { if (Model.OnForeground == value) return; Model.OnForeground = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveForeground)); } }
    public string OnBackground { get => Model.OnBackground; set { if (Model.OnBackground == value) return; Model.OnBackground = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveBackground)); } }
    public string OffForeground { get => Model.OffForeground; set { if (Model.OffForeground == value) return; Model.OffForeground = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveForeground)); } }
    public string OffBackground { get => Model.OffBackground; set { if (Model.OffBackground == value) return; Model.OffBackground = value; OnPropertyChanged(); OnPropertyChanged(nameof(EffectiveBackground)); } }
    public bool IsVisible { get => Model.IsVisible; set { if (Model.IsVisible == value) return; Model.IsVisible = value; OnPropertyChanged(); } }
    public bool IsEnabled { get => Model.IsEnabled; set { if (Model.IsEnabled == value) return; Model.IsEnabled = value; OnPropertyChanged(); } }
    public double Minimum { get => Model.Minimum; set { if (Math.Abs(Model.Minimum - value) < 0.01) return; Model.Minimum = value; OnPropertyChanged(); } }
    public double Maximum { get => Model.Maximum; set { if (Math.Abs(Model.Maximum - value) < 0.01) return; Model.Maximum = value; OnPropertyChanged(); } }
    public string LiveValue { get => _liveValue; set { if (!SetProperty(ref _liveValue, value)) return; OnPropertyChanged(nameof(DisplayValue)); OnPropertyChanged(nameof(EffectiveCaption)); OnPropertyChanged(nameof(IsOn)); OnPropertyChanged(nameof(LedBrush)); OnPropertyChanged(nameof(EffectiveForeground)); OnPropertyChanged(nameof(EffectiveBackground)); } }
    public string EffectiveForeground => DynamicStateColors ? (IsOn ? OnForeground : OffForeground) : Foreground;
    public string EffectiveBackground => DynamicStateColors ? (IsOn ? OnBackground : OffBackground) : Background;
    public string EffectiveCaption => Type == ScadaWidgetType.Button ? (IsOn ? OnCaption : OffCaption) : Caption;
    public string DisplayValue => Type == ScadaWidgetType.Value ? string.IsNullOrWhiteSpace(Unit) || LiveValue == "--" ? LiveValue : $"{LiveValue} {Unit}" : EffectiveCaption;
    public bool IsTextVisible => Type is ScadaWidgetType.Label or ScadaWidgetType.Value;
    public bool IsButtonVisible => Type == ScadaWidgetType.Button;
    public bool IsLedVisible => Type == ScadaWidgetType.Led;
    public bool IsRectangleVisible => Type == ScadaWidgetType.Rectangle;
    public bool IsCircleVisible => Type == ScadaWidgetType.Circle;
    public bool IsLineVisible => Type == ScadaWidgetType.Line;
    public bool IsOn => LiveValue.Equals("ON", StringComparison.OrdinalIgnoreCase) || LiveValue.Equals("true", StringComparison.OrdinalIgnoreCase) || double.TryParse(LiveValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && Math.Abs(n) > double.Epsilon;
    public string LedBrush => DynamicStateColors ? EffectiveBackground : (IsOn ? "#39E68A" : "#566D80");
    public void ApplyColor(string target, string color)
    {
        switch (target)
        {
            case "Foreground": Foreground = color; break;
            case "Background": Background = color; break;
            case "ON foreground": OnForeground = color; break;
            case "ON background": OnBackground = color; break;
            case "OFF foreground": OffForeground = color; break;
            case "OFF background": OffBackground = color; break;
        }
    }
    public void MoveTo(double x, double y) { X = x; Y = y; }
    private void RaiseVisualProperties()
    {
        OnPropertyChanged(nameof(DisplayValue));
        OnPropertyChanged(nameof(IsTextVisible));
        OnPropertyChanged(nameof(IsButtonVisible));
        OnPropertyChanged(nameof(IsLedVisible));
        OnPropertyChanged(nameof(IsRectangleVisible));
        OnPropertyChanged(nameof(IsCircleVisible));
        OnPropertyChanged(nameof(IsLineVisible));
    }
}
