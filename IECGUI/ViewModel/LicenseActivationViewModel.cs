using IEC.Shared.Services;
using IECGUI.Services;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace IECGUI.ViewModel;

public sealed class LicenseActivationViewModel : BaseViewModel
{
    private readonly LicenseService _license;
    private readonly INavigationService _navigation;
    private readonly DeviceRuntimeService _deviceRuntime;
    private readonly AlarmMonitoringService _alarmService;
    private string _productKey = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    public LicenseActivationViewModel(
        LicenseService license,
        INavigationService navigation,
        DeviceRuntimeService deviceRuntime,
        AlarmMonitoringService alarmService)
    {
        _license = license;
        _navigation = navigation;
        _deviceRuntime = deviceRuntime;
        _alarmService = alarmService;

        ActivateCommand = new RelayCommand(async () => await ActivateAsync(), () => !IsBusy);
        ExitCommand = new RelayCommand(() => Application.Current.Shutdown());

        var current = _license.Validate();
        StatusMessage = current.Message;
    }

    public string ProductKey
    {
        get => _productKey;
        set => SetProperty(ref _productKey, value);
    }

    public string InstallationId => _license.InstallationId;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public ICommand ActivateCommand { get; }
    public ICommand ExitCommand { get; }

    private async Task ActivateAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            if (!_license.TryActivate(ProductKey, out var error))
            {
                StatusMessage = error;
                return;
            }

            StatusMessage = "License accepted. Connecting to configured devices...";
            await _deviceRuntime.StartAsync();
            await _alarmService.StartAsync();
            _navigation.NavigateTo<LoginViewModel>();
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"License accepted, but device startup failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
