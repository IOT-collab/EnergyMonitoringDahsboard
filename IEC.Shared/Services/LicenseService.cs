using Microsoft.Win32;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IEC.Shared.Services;

/// <summary>
/// Application licensing for the offline trial/product-key workflow.
///
/// The installer writes only the installation marker. Product keys are signed by
/// the vendor and are verified with the public key embedded in this assembly.
/// Never ship the private signing key with the application.
/// </summary>
public sealed class LicenseService
{
    private const string RegistryPath = @"Software\Vertex Automation System\VEMT";
    private const string InstallUtcValue = "InstallUtc";
    private const string StateFileName = "LicenseState.json";
    private const int TrialDays = 30;
    private static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(5);

    // Replace this with the public key matching the private key kept by the vendor.
    // The private key must never be placed in the application or installer.
    private const string VendorPublicKeyPem = "-----BEGIN PUBLIC KEY-----\nMIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEAlvU6LlH3aoHEy/ZwPHzp\nfCT2yUv1gZAubOmUXrXAIbNfa0Q2tAkCc1Wv8JL8lp7xQGMOa80L/syYS2TYg8c6\nV4rAMMySXr1OgcSltjtOUhiAR1TcgsR0ePjWTgN+obH2KaSyvyhMBVJTAKmW5k9C\npQmnM0xTSzUwfxvQXJ3mQ4kHJ0dFIg9fOX82DNVf2+tFHlajm8PWVhnLVnYfXL58\n/hHuLqXZKMBxI2R1MZJpxnpjdxkLHvQO28vgCdIIIzZnXZF51KfsCuvF+bAnPxr9\nx0jtemktzRw/B7G1D81znlws0xJBJKsHSrORZ0BEp7RfpOjF5Uc86EeFLtb3R9U9\nsH0BBlTTx2QfTTJGWP4aZ5lTxSqQoQJUBbpwWOw+6QjCsj7Bx9bRJq/zDrH6hA7H\nFw6RlTA7JsZ03AmeFVbjSrS9cT5h1XoBcF4ajVTWQzpU//hS82PqXJbwadw1CXHH\nuU537lRKAZNuynvXQ1hO67ewPCOvgFNQ46HdzgGIiBIDAgMBAAE=\n-----END PUBLIC KEY-----";

    private readonly object _sync = new();
    private LicenseState _state;
    private LicenseStatus _current;

    public LicenseService()
    {
        _state = LoadState() ?? CreateInitialState();
        _current = EvaluateAndPersist();
    }

    public string InstallationId
    {
        get { lock (_sync) return _state.InstallationId; }
    }

    public LicenseStatus Current
    {
        get { lock (_sync) return _current; }
    }

    public event Action<LicenseStatus>? StatusChanged;

    public LicenseStatus Validate()
    {
        lock (_sync)
        {
            _current = EvaluateAndPersist();
            var status = _current;
            StatusChanged?.Invoke(status);
            return status;
        }
    }

    public bool TryActivate(string productKey, out string error)
    {
        lock (_sync)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(productKey))
            {
                error = "Enter a product key.";
                return false;
            }

            if (!TryReadSignedKey(productKey.Trim(), out var payload, out error))
                return false;

            if (!string.Equals(payload.Product, AppInfo.Product, StringComparison.OrdinalIgnoreCase))
            {
                error = "This product key belongs to a different product.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(payload.InstallationId) &&
                !string.Equals(payload.InstallationId, _state.InstallationId, StringComparison.OrdinalIgnoreCase))
            {
                error = "This product key was issued for another installation.";
                return false;
            }

            if (!Enum.TryParse<LicenseKind>(payload.Type, true, out var kind))
            {
                error = "The product key contains an unsupported license type.";
                return false;
            }

            var now = DateTimeOffset.UtcNow;
            if (!DateTimeOffset.TryParse(payload.IssuedUtc, out var issued) || issued > now + ClockTolerance)
            {
                error = "The product key issue date is invalid.";
                return false;
            }

            DateTimeOffset? expires = null;
            if (kind == LicenseKind.Annual)
            {
                if (!DateTimeOffset.TryParse(payload.ExpiresUtc, out var parsedExpiry) || parsedExpiry <= now)
                {
                    error = "The annual product key has expired.";
                    return false;
                }

                expires = parsedExpiry.ToUniversalTime();
            }

            _state.License = new StoredLicense
            {
                Token = productKey.Trim(),
                Kind = kind,
                IssuedUtc = issued.ToUniversalTime(),
                ExpiresUtc = expires
            };
            _state.LastSeenUtc = now;
            SaveState(_state);
            _current = EvaluateAndPersist();
            StatusChanged?.Invoke(_current);
            return _current.CanRun;
        }
    }

    private LicenseStatus EvaluateAndPersist()
    {
        var now = DateTimeOffset.UtcNow;
        var rollbackDetected = _state.LastSeenUtc.HasValue && now + ClockTolerance < _state.LastSeenUtc.Value;

        if (!rollbackDetected && (!_state.LastSeenUtc.HasValue || now > _state.LastSeenUtc.Value))
        {
            _state.LastSeenUtc = now;
            SaveState(_state);
        }

        if (rollbackDetected)
        {
            return new LicenseStatus(
                LicenseStateKind.ClockTampered,
                false,
                "The system clock appears to have moved backwards. Correct the clock or reactivate the product.",
                _state.InstallUtc.AddDays(TrialDays));
        }

        // Never trust the cached license metadata alone. The state file is local
        // and can be edited, so verify the signed token again on every launch.
        if (_state.License is { } storedLicense &&
            TryReadSignedKey(storedLicense.Token, out var signedPayload, out _ ) &&
            Enum.TryParse<LicenseKind>(signedPayload.Type, true, out var signedKind) &&
            string.Equals(signedPayload.Product, AppInfo.Product, StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(signedPayload.InstallationId) ||
             string.Equals(signedPayload.InstallationId, _state.InstallationId, StringComparison.OrdinalIgnoreCase)))
        {
            if (signedKind == LicenseKind.Lifetime)
            {
                return new LicenseStatus(LicenseStateKind.Licensed, true, "Lifetime license active.", null);
            }

            if (signedKind == LicenseKind.Annual &&
                DateTimeOffset.TryParse(signedPayload.ExpiresUtc, out var signedExpiry) && signedExpiry > now)
            {
                return new LicenseStatus(LicenseStateKind.Licensed, true,
                    $"Annual license active until {signedExpiry:yyyy-MM-dd}.", signedExpiry.ToUniversalTime());
            }

            var expiry = DateTimeOffset.TryParse(signedPayload.ExpiresUtc, out var parsedExpiry)
                ? parsedExpiry.ToUniversalTime()
                : (DateTimeOffset?)null;
            return new LicenseStatus(LicenseStateKind.Expired, false, "The product license has expired.", expiry);
        }

        if (_state.License is not null)
        {
            // Discard a tampered or unverifiable cached license and continue with
            // the original trial date instead of allowing local metadata to grant access.
            _state.License = null;
            SaveState(_state);
        }

        var trialExpiry = _state.InstallUtc.AddDays(TrialDays);
        if (now < trialExpiry)
        {
            var remaining = Math.Max(0, (int)Math.Ceiling((trialExpiry - now).TotalDays));
            return new LicenseStatus(LicenseStateKind.Trial, true,
                $"Trial license: {remaining} day(s) remaining.", trialExpiry);
        }

        return new LicenseStatus(LicenseStateKind.Expired, false, "The 30-day trial has expired.", trialExpiry);
    }

    private LicenseState CreateInitialState()
    {
        var installUtc = ReadInstallerInstallUtc() ?? DateTimeOffset.UtcNow;
        var state = new LicenseState
        {
            InstallationId = Guid.NewGuid().ToString("N"),
            InstallUtc = installUtc,
            LastSeenUtc = DateTimeOffset.UtcNow
        };
        SaveState(state);
        return state;
    }

    private static DateTimeOffset? ReadInstallerInstallUtc()
    {
        try
        {
            // HKA in Inno Setup can resolve to either registry view. Try both so
            // AnyCPU/32-bit and 64-bit published builds use the same marker.
            foreach (var view in new[] { RegistryView.Default, RegistryView.Registry64, RegistryView.Registry32 })
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var key = baseKey.OpenSubKey(RegistryPath, writable: false);
                var value = key?.GetValue(InstallUtcValue)?.ToString();
                if (!string.IsNullOrWhiteSpace(value) && DateTimeOffset.TryParse(value, out var parsed))
                    return parsed.ToUniversalTime();
            }
        }
        catch
        {
            // Registry access is best-effort. The application can still initialize a trial.
        }

        return null;
    }

    private static LicenseState? LoadState()
    {
        try
        {
            if (!File.Exists(AppPaths.LicenseStateFile)) return null;
            return JsonSerializer.Deserialize<LicenseState>(File.ReadAllText(AppPaths.LicenseStateFile));
        }
        catch
        {
            return null;
        }
    }

    private static void SaveState(LicenseState state)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.Configuration);
            var temp = AppPaths.LicenseStateFile + ".tmp";
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(temp, json);
            File.Move(temp, AppPaths.LicenseStateFile, overwrite: true);
        }
        catch
        {
            // A read-only configuration directory should not crash application startup.
        }
    }

    private static bool TryReadSignedKey(string token, out LicensePayload payload, out string error)
    {
        payload = new LicensePayload();
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(VendorPublicKeyPem))
        {
            error = "License signing key is not configured. Contact the software vendor.";
            return false;
        }

        var parts = token.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            error = "Invalid product key format.";
            return false;
        }

        try
        {
            var payloadBytes = Base64UrlDecode(parts[0]);
            var signatureBytes = Base64UrlDecode(parts[1]);
            using var rsa = RSA.Create();
            rsa.ImportFromPem(VendorPublicKeyPem);

            if (!rsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            {
                error = "Product key signature is invalid.";
                return false;
            }

            payload = JsonSerializer.Deserialize<LicensePayload>(payloadBytes)
                ?? throw new InvalidDataException("Empty license payload.");
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or CryptographicException or JsonException or InvalidDataException)
        {
            error = "Product key could not be verified.";
            return false;
        }
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 += new string('=', (4 - base64.Length % 4) % 4);
        return Convert.FromBase64String(base64);
    }
}

public enum LicenseKind
{
    Annual,
    Lifetime
}

public enum LicenseStateKind
{
    Trial,
    Licensed,
    Expired,
    ClockTampered
}

public sealed record LicenseStatus(
    LicenseStateKind State,
    bool CanRun,
    string Message,
    DateTimeOffset? ExpiresUtc);

public sealed class LicensePayload
{
    public string Product { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string IssuedUtc { get; set; } = string.Empty;
    public string? ExpiresUtc { get; set; }
    public string? InstallationId { get; set; }
}

internal sealed class LicenseState
{
    public string InstallationId { get; set; } = string.Empty;
    public DateTimeOffset InstallUtc { get; set; }
    public DateTimeOffset? LastSeenUtc { get; set; }
    public StoredLicense? License { get; set; }
}

internal sealed class StoredLicense
{
    public string Token { get; set; } = string.Empty;
    public LicenseKind Kind { get; set; }
    public DateTimeOffset IssuedUtc { get; set; }
    public DateTimeOffset? ExpiresUtc { get; set; }
}
