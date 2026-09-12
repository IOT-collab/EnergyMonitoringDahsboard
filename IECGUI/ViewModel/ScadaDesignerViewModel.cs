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
    private readonly IDialogService _dialog;
    private ScadaPageConfig _selectedPage;
    private ScadaWidgetViewModel _selectedWidget;
    private string _status = "Create a mimic page by adding objects to the canvas.";

    public ObservableCollection<ScadaPageConfig> Pages { get; } = new();
    public ObservableCollection<ScadaWidgetViewModel> Widgets { get; } = new();
    public ObservableCollection<string> AvailableDevices { get; } = new();
    public ObservableCollection<string> AvailableParameters { get; } = new();

    public ScadaPageConfig SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (!SetProperty(ref _selectedPage, value) || value == null) return;
            LoadSelectedPage();
        }
    }

    public ScadaWidgetViewModel SelectedWidget
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

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

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
    public ICommand SaveCommand { get; }
    public ICommand BackCommand { get; }

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
        _dialog = dialog;

        foreach (var meter in (_configuration.Configuration.Meters ?? new())
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.MeterName))
            .OrderBy(x => x.MeterName))
        {
            AvailableDevices.Add(meter.MeterName);
        }

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
        SaveCommand = new RelayCommand(Save);
        BackCommand = new RelayCommand(() => _navigation.NavigateTo<HomePageViewModel>());

        foreach (var page in _layouts.LoadPages())
            Pages.Add(page);

        SelectedPage = Pages.FirstOrDefault();
        _runtime.SnapshotUpdated += OnSnapshotUpdated;
        RefreshLiveValues();
    }

    private void LoadSelectedPage()
    {
        Widgets.Clear();
        if (SelectedPage == null) return;

        foreach (var widget in SelectedPage.Widgets ?? new())
            Widgets.Add(new ScadaWidgetViewModel(widget));

        SelectedWidget = Widgets.FirstOrDefault();
        OnPropertyChanged(nameof(CanvasWidth));
        OnPropertyChanged(nameof(CanvasHeight));
        RefreshLiveValues();
    }

    private void NewPage()
    {
        var page = new ScadaPageConfig { PageName = $"Page {Pages.Count + 1}" };
        Pages.Add(page);
        SelectedPage = page;
        Status = $"Created {page.PageName}.";
    }

    private void DeletePage()
    {
        if (SelectedPage == null || Pages.Count <= 1) return;
        var name = SelectedPage.PageName;
        Pages.Remove(SelectedPage);
        SelectedPage = Pages.FirstOrDefault();
        Status = $"Deleted {name}. Press SAVE to persist the change.";
    }

    private void AddWidget(ScadaWidgetType type)
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
            X = 80 + (index % 5) * 250,
            Y = 70 + (index / 5) * 105,
            Width = type == ScadaWidgetType.Line ? 220 : 210,
            Height = type == ScadaWidgetType.Line ? 8 : type == ScadaWidgetType.Circle ? 100 : 58,
            DeviceName = AvailableDevices.FirstOrDefault(),
            ParameterName = AvailableParameters.FirstOrDefault()
        };

        var item = new ScadaWidgetViewModel(config);
        SelectedPage.Widgets.Add(config);
        Widgets.Add(item);
        SelectedWidget = item;
        Status = $"Added {type}. Drag it on the canvas and configure its data source.";
    }

    private void DeleteSelectedWidget()
    {
        if (SelectedWidget == null || SelectedPage == null) return;
        var removed = SelectedWidget;
        Widgets.Remove(removed);
        SelectedPage.Widgets.Remove(removed.Model);
        SelectedWidget = Widgets.FirstOrDefault();
        Status = "Object removed. Press SAVE to persist the change.";
    }

    private void Save()
    {
        if (SelectedPage == null) return;
        foreach (var page in Pages)
            page.Widgets = page == SelectedPage
                ? Widgets.Select(x => x.Model).ToList()
                : page.Widgets ?? new();

        if (_layouts.SavePages(Pages))
            Status = $"Saved {Pages.Count} page(s) to the project configuration.";
        else
            Status = "Unable to save the SCADA layout.";
    }

    private void RefreshParameters()
    {
        AvailableParameters.Clear();
        var device = _configuration.Configuration.Meters?.FirstOrDefault(x =>
            string.Equals(x.MeterName, SelectedWidget?.DeviceName, StringComparison.OrdinalIgnoreCase));
        foreach (var register in device?.Registers?.Where(x => x != null && x.IsEnabled) ?? Enumerable.Empty<RegisterConfig>())
        {
            var name = string.IsNullOrWhiteSpace(register.ParameterName)
                ? register.RegisterAddress.ToString(CultureInfo.InvariantCulture)
                : register.ParameterName;
            if (!AvailableParameters.Contains(name, StringComparer.OrdinalIgnoreCase))
                AvailableParameters.Add(name);
        }
    }

    private void OnSnapshotUpdated() => RunOnUi(RefreshLiveValues);

    private void RefreshLiveValues()
    {
        var snapshot = _runtime.GetSnapshot();
        foreach (var widget in Widgets)
        {
            if (string.IsNullOrWhiteSpace(widget.DeviceName) || string.IsNullOrWhiteSpace(widget.ParameterName))
            {
                widget.LiveValue = "--";
                continue;
            }

            if (!snapshot.TryGetValue(widget.DeviceName, out var reading) || reading?.Values == null)
            {
                widget.LiveValue = "--";
                continue;
            }

            var match = reading.Values.FirstOrDefault(x =>
                string.Equals(x.Key, widget.ParameterName, StringComparison.OrdinalIgnoreCase));
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
        if (dispatcher == null || dispatcher.CheckAccess()) action();
        else dispatcher.BeginInvoke(action);
    }
}

public sealed class ScadaWidgetViewModel : ObservableObjectVM
{
    public ScadaWidgetConfig Model { get; }
    private string _liveValue = "--";

    public ScadaWidgetViewModel(ScadaWidgetConfig model) => Model = model;

    public string Id => Model.Id;
    public ScadaWidgetType Type { get => Model.Type; set { if (Model.Type == value) return; Model.Type = value; OnPropertyChanged(); RaiseVisualProperties(); } }
    public string Caption { get => Model.Caption; set { if (Model.Caption == value) return; Model.Caption = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayValue)); } }
    public double X { get => Model.X; set { if (Math.Abs(Model.X - value) < 0.01) return; Model.X = value; OnPropertyChanged(); } }
    public double Y { get => Model.Y; set { if (Math.Abs(Model.Y - value) < 0.01) return; Model.Y = value; OnPropertyChanged(); } }
    public double Width { get => Model.Width; set { if (Math.Abs(Model.Width - value) < 0.01) return; Model.Width = Math.Max(0, value); OnPropertyChanged(); } }
    public double Height { get => Model.Height; set { if (Math.Abs(Model.Height - value) < 0.01) return; Model.Height = Math.Max(0, value); OnPropertyChanged(); } }
    public double Rotation { get => Model.Rotation; set { if (Math.Abs(Model.Rotation - value) < 0.01) return; Model.Rotation = value; OnPropertyChanged(); } }
    public string DeviceName { get => Model.DeviceName ?? string.Empty; set { if (Model.DeviceName == value) return; Model.DeviceName = value; OnPropertyChanged(); } }
    public string ParameterName { get => Model.ParameterName ?? string.Empty; set { if (Model.ParameterName == value) return; Model.ParameterName = value; OnPropertyChanged(); } }
    public string Unit { get => Model.Unit; set { if (Model.Unit == value) return; Model.Unit = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayValue)); } }
    public string Foreground { get => Model.Foreground; set { if (Model.Foreground == value) return; Model.Foreground = value; OnPropertyChanged(); } }
    public string Background { get => Model.Background; set { if (Model.Background == value) return; Model.Background = value; OnPropertyChanged(); } }
    public bool IsVisible { get => Model.IsVisible; set { if (Model.IsVisible == value) return; Model.IsVisible = value; OnPropertyChanged(); } }
    public bool IsEnabled { get => Model.IsEnabled; set { if (Model.IsEnabled == value) return; Model.IsEnabled = value; OnPropertyChanged(); } }
    public double Minimum { get => Model.Minimum; set { if (Math.Abs(Model.Minimum - value) < 0.01) return; Model.Minimum = value; OnPropertyChanged(); } }
    public double Maximum { get => Model.Maximum; set { if (Math.Abs(Model.Maximum - value) < 0.01) return; Model.Maximum = value; OnPropertyChanged(); } }

    public string LiveValue
    {
        get => _liveValue;
        set
        {
            if (!SetProperty(ref _liveValue, value)) return;
            OnPropertyChanged(nameof(DisplayValue));
            OnPropertyChanged(nameof(IsOn));
            OnPropertyChanged(nameof(LedBrush));
        }
    }

    public string DisplayValue => Type == ScadaWidgetType.Value
        ? string.IsNullOrWhiteSpace(Unit) || LiveValue == "--" ? LiveValue : $"{LiveValue} {Unit}"
        : Caption;
    public bool IsTextVisible => Type is ScadaWidgetType.Label or ScadaWidgetType.Value or ScadaWidgetType.Button;
    public bool IsLedVisible => Type == ScadaWidgetType.Led;
    public bool IsRectangleVisible => Type == ScadaWidgetType.Rectangle;
    public bool IsCircleVisible => Type == ScadaWidgetType.Circle;
    public bool IsLineVisible => Type == ScadaWidgetType.Line;
    public bool IsOn => LiveValue.Equals("ON", StringComparison.OrdinalIgnoreCase) || LiveValue.Equals("true", StringComparison.OrdinalIgnoreCase) || double.TryParse(LiveValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && Math.Abs(n) > double.Epsilon;
    public string LedBrush => IsOn ? "#39E68A" : "#566D80";

    public void MoveTo(double x, double y)
    {
        X = Math.Max(0, x);
        Y = Math.Max(0, y);
    }

    private void RaiseVisualProperties()
    {
        OnPropertyChanged(nameof(DisplayValue));
        OnPropertyChanged(nameof(IsTextVisible));
        OnPropertyChanged(nameof(IsLedVisible));
        OnPropertyChanged(nameof(IsRectangleVisible));
        OnPropertyChanged(nameof(IsCircleVisible));
        OnPropertyChanged(nameof(IsLineVisible));
    }
}

