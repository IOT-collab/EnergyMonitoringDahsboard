using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

const string product = "VEMT";
var options = ParseArguments(args);

if (!options.TryGetValue("private-key", out var privateKeyPath) ||
    !options.TryGetValue("installation-id", out var installationId) ||
    !options.TryGetValue("type", out var type))
{
    PrintUsage();
    return 2;
}

if (!File.Exists(privateKeyPath))
{
    Console.Error.WriteLine($"Private key file was not found: {privateKeyPath}");
    return 3;
}

if (!type.Equals("Annual", StringComparison.OrdinalIgnoreCase) &&
    !type.Equals("Lifetime", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("License type must be Annual or Lifetime.");
    return 4;
}

type = char.ToUpperInvariant(type[0]) + type[1..].ToLowerInvariant();
var issuedUtc = DateTimeOffset.UtcNow;
DateTimeOffset? expiresUtc = null;

if (type == "Annual")
{
    if (options.TryGetValue("expires", out var expiresText))
    {
        if (!DateTimeOffset.TryParse(expiresText, out var parsedExpiry))
        {
            Console.Error.WriteLine("The --expires value is not a valid date/time.");
            return 5;
        }

        expiresUtc = parsedExpiry.ToUniversalTime();
    }
    else
    {
        expiresUtc = issuedUtc.AddDays(365);
    }

    if (expiresUtc <= issuedUtc)
    {
        Console.Error.WriteLine("Annual license expiry must be in the future.");
        return 6;
    }
}

var payload = new
{
    Product = product,
    Type = type,
    IssuedUtc = issuedUtc.ToString("O"),
    ExpiresUtc = expiresUtc?.ToString("O"),
    InstallationId = installationId
};

var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
using var rsa = RSA.Create();
rsa.ImportFromPem(File.ReadAllText(privateKeyPath));
var signature = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
var licenseKey = $"{Base64UrlEncode(payloadBytes)}.{Base64UrlEncode(signature)}";

if (options.TryGetValue("output", out var outputPath))
{
    File.WriteAllText(outputPath, licenseKey + Environment.NewLine, Encoding.UTF8);
    Console.WriteLine($"License key written to: {outputPath}");
}
else
{
    Console.WriteLine(licenseKey);
}

return 0;

static Dictionary<string, string> ParseArguments(string[] args)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        if (!args[i].StartsWith("--", StringComparison.Ordinal) || i + 1 >= args.Length)
            continue;

        result[args[i][2..]] = args[++i];
    }

    return result;
}

static string Base64UrlEncode(byte[] bytes) =>
    Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

static void PrintUsage()
{
    Console.WriteLine("VEMT offline license-key generator");
    Console.WriteLine();
    Console.WriteLine("Lifetime license:");
    Console.WriteLine("  dotnet run --project Tools/LicenseKeyGenerator -- --private-key C:\\VEMT-License\\VEMT_private.pem --installation-id ID --type Lifetime");
    Console.WriteLine();
    Console.WriteLine("Annual license (defaults to 365 days):");
    Console.WriteLine("  dotnet run --project Tools/LicenseKeyGenerator -- --private-key C:\\VEMT-License\\VEMT_private.pem --installation-id ID --type Annual");
    Console.WriteLine();
    Console.WriteLine("Optional: --expires 2027-08-18T00:00:00Z --output C:\\VEMT-License\\license.txt");
}
