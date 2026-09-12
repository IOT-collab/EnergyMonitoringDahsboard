using IEC.Shared.Models;
using IEC.Shared.Services;
using IECGUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IECGUI.ViewModel;

public sealed class ScadaRuntimeViewModel : BaseViewModel
{
    private readonly ConfigurationManagerService _configuration;
    private readonly DeviceRuntimeService _runtime;
    private readonly ScadaLayoutService _layouts;
    private readonly ScadaWriteService _writer;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialog;
    private ScadaPageConfig _selectedPage;
    private string _status = "Runtime view is locked. Values are read from configured devices.";

    public ObservableCollection<ScadaPageConfig> Pages { get; } = new();
    public ObservableCollection<ScadaWidgetViewModel> Widgets { get; } = new();
    public ScadaPageConfig SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (!SetProperty(ref _selectedPage, value) || value == null) return;
            Widgets.Clear();
            foreach (var widget in value.Widgets ?? new()) Widgets.Add(new ScadaWidgetViewModel(widget));
            OnPropertyChanged(nameof(CanvasWidth));
            OnPropertyChanged(nameof(CanvasHeight));
            RefreshLiveValues();
        }
    }

    public double CanvasWidth => SelectedPage?.CanvasWidth ?? 1500;
    public double CanvasHeight => SelectedPage?.CanvasHeight ?? 800;
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public ICommand WriteCommand { get; }
    public ICommand BackCommand { get; }

    public ScadaRuntimeViewModel(
        ConfigurationManagerService configuration,
        DeviceRuntimeService runtime,
        ScadaLayoutService layouts,
        ScadaWriteService writer,
        INavigationService navigation,
        IDialogService dialog)
    {
        _configuration = configuration;
        _runtime = runtime;
        _layouts = layouts;
        _writer = writer;
        _navigation = navigation;
        _dialog = dialog;
        WriteCommand = new RelayCommand<ScadaWidgetViewModel>(async widget => await WriteAsync(widget));
        BackCommand = new RelayCommand(() => _navigation.NavigateTo<HomePageViewModel>());

        foreach (var page in _layouts.LoadPages()) Pages.Add(page);
        SelectedPage = Pages.FirstOrDefault();
        _runtime.SnapshotUpdated += OnSnapshotUpdated;
        RefreshLiveValues();
    }

    private async Task WriteAsync(ScadaWidgetViewModel widget)
    {
        if (widget == null || !widget.IsButtonVisible || !widget.IsEnabled) return;
        var desired = !widget.IsOn;
        if (!_dialog.ShowYesNo($"Confirm {(desired ? "ON" : "OFF")} command for '{widget.Caption}'?", "SCADA command")) return;

        try
        {
            await _writer.WriteToggleAsync(widget, desired);
            widget.LiveValue = desired ? "ON" : "OFF";
            Status = $"{widget.Caption}: command completed at {DateTime.Now:HH:mm:ss}.";
        }
        catch (Exception ex)
        {
            Status = $"Command failed: {ex.Message}";
            _dialog.ShowWarning(Status);
        }
    }

    private void OnSnapshotUpdated() => RunOnUi(RefreshLiveValues);

    private void RefreshLiveValues()
    {
        var snapshot = _runtime.GetSnapshot();
        foreach (var widget in Widgets)
        {
            if (string.IsNullOrWhiteSpace(widget.DeviceName) || string.IsNullOrWhiteSpace(widget.ParameterName) ||
                !snapshot.TryGetValue(widget.DeviceName, out var reading) || reading?.Values == null)
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
