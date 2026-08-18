using IEC.Shared.Services;
using IECGUI.Services;
using Microsoft.Win32;
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
    private readonly IDialogService _dialog;
    private string _productKey = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isBusy;

    public LicenseActivationViewModel(
        LicenseService license,
        INavigationService navigation,
        DeviceRuntimeService deviceRuntime,
        AlarmMonitoringService alarmService,
        IDialogService dialog)
    {
        _license = license;
        _navigation = navigation;
        _deviceRuntime = deviceRuntime;
        _alarmService = alarmService;
        _dialog = dialog;

        ActivateCommand = new RelayCommand(async () => await ActivateAsync(ProductKey), () => !IsBusy);
        ImportLicenseFileCommand = new RelayCommand(async () => await ImportLicenseFileAsync(), () => !IsBusy);
        CopyInstallationIdCommand = new RelayCommand(CopyInstallationId);
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
    public ICommand ImportLicenseFileCommand { get; }
    public ICommand CopyInstallationIdCommand { get; }
    public ICommand ExitCommand { get; }

    private void CopyInstallationId()
    {
        try
        {
            Clipboard.SetText(InstallationId);
            StatusMessage = "Installation ID copied to the clipboard.";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Unable to copy the Installation ID: {ex.Message}";
        }
    }

    private async Task ImportLicenseFileAsync()
    {
        if (IsBusy) return;

        var dialog = new OpenFileDialog
        {
            Title = "Select VEMT license file",
            Filter = "VEMT license (*.lic)|*.lic|Text license (*.txt)|*.txt|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            IsBusy = true;
            if (!_license.TryActivateFromFile(dialog.FileName, out var error))
            {
                StatusMessage = error;
                return;
            }

            await FinishActivationAsync();
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"License file could not be activated: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ActivateAsync(string productKey)
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

            await FinishActivationAsync();
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

    private async Task FinishActivationAsync()
    {
        _dialog.ShowMessage(
            "License activation completed successfully.",
            "Activation Successful");
        StatusMessage = "License accepted. Connecting to configured devices...";
        await _deviceRuntime.StartAsync();
        await _alarmService.StartAsync();
        _navigation.NavigateTo<LoginViewModel>();
    }
}
