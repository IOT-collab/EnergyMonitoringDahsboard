using IEC.Shared.Services;
using IECGUI.Services;
using System;
using System.Windows;
using System.Windows.Input;

namespace IECGUI.ViewModel;

/// <summary>
/// Read-only license summary shown to every signed-in user.
/// Activation remains restricted to the startup license gate.
/// </summary>
public sealed class LicenseInfoViewModel : BaseViewModel
{
    private readonly LicenseService _license;
    private readonly INavigationService _navigation;
    private LicenseInfo _info;

    public LicenseInfoViewModel(LicenseService license, INavigationService navigation)
    {
        _license = license;
        _navigation = navigation;
        _info = _license.GetInfo();

        BackCommand = new RelayCommand(() => _navigation.NavigateTo<HomePageViewModel>());
        RefreshCommand = new RelayCommand(Refresh);
        _license.StatusChanged += OnLicenseStatusChanged;
    }

    public ICommand BackCommand { get; }
    public ICommand RefreshCommand { get; }

    public string Product => _info.Product;
    public string InstallationId => _info.InstallationId;
    public string InstalledOn => _info.InstalledUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    public string Status => _info.Status.State switch
    {
        LicenseStateKind.Trial => "Trial",
        LicenseStateKind.Licensed => "Licensed",
        LicenseStateKind.Expired => "Expired",
        LicenseStateKind.ClockTampered => "Clock warning",
        _ => _info.Status.State.ToString()
    };
    public string StatusMessage => _info.Status.Message;
    public string LicenseType => _info.Kind switch
    {
        LicenseKind.Lifetime => "Lifetime",
        LicenseKind.Annual => "Annual",
        _ => "Trial"
    };
    public string IssuedOn => _info.IssuedUtc.HasValue
        ? _info.IssuedUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
        : "—";
    public string ExpiresOn => _info.ExpiresUtc.HasValue
        ? _info.ExpiresUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
        : (LicenseType == "Lifetime" ? "Never" : "—");
    public string TimeRemaining
    {
        get
        {
            if (LicenseType == "Lifetime") return "Unlimited";
            if (!_info.ExpiresUtc.HasValue) return "Unavailable";

            var remaining = _info.ExpiresUtc.Value - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero) return "Expired";
            return $"{Math.Max(0, (int)Math.Ceiling(remaining.TotalDays))} day(s)";
        }
    }
    public string ProductKeyInfo => _info.ProductKeyHint ?? "Not activated (trial license)";

    private void OnLicenseStatusChanged(LicenseStatus _)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess()) Refresh();
        else dispatcher.BeginInvoke(Refresh);
    }

    private void Refresh()
    {
        _info = _license.GetInfo();
        OnPropertyChanged(nameof(Product));
        OnPropertyChanged(nameof(InstallationId));
        OnPropertyChanged(nameof(InstalledOn));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(LicenseType));
        OnPropertyChanged(nameof(IssuedOn));
        OnPropertyChanged(nameof(ExpiresOn));
        OnPropertyChanged(nameof(TimeRemaining));
        OnPropertyChanged(nameof(ProductKeyInfo));
    }
}
