using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PortalSsoSender;

var environmentOverrides = ReadEnvironmentOverrides();
var projectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
var explicitConfigPath = GetExplicitConfigPath(args);

var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.sample.json"), optional: true)
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true)
    .AddJsonFile(Path.Combine(projectDirectory, "appsettings.sample.json"), optional: true)
    .AddJsonFile(Path.Combine(projectDirectory, "appsettings.json"), optional: true)
    .AddInMemoryCollection(environmentOverrides);

if (!string.IsNullOrWhiteSpace(explicitConfigPath))
    configurationBuilder.AddJsonFile(explicitConfigPath, optional: false);

var configuration = configurationBuilder.Build();

var options = configuration.Get<PortalSsoSenderOptions>() ?? new PortalSsoSenderOptions();
HydrateKeyMaterial(options);

if (HasPlaceholderConfiguration(options))
{
    Console.Error.WriteLine("Portal SSO sender is not configured. Copy appsettings.sample.json to appsettings.json and fill in the real values.");
    return 1;
}

var sender = new PortalSsoSender.PortalSsoSender();
var encryptedPayload = sender.BuildEncryptedPayload(options);

Console.WriteLine("Generated encrypted payload:");
Console.WriteLine(encryptedPayload);
Console.WriteLine();

var callbackResponse = await sender.SendCallbackAsync(options, encryptedPayload);
var callbackBody = await callbackResponse.Content.ReadAsStringAsync();

Console.WriteLine($"Callback response: {(int)callbackResponse.StatusCode} {callbackResponse.StatusCode}");
Console.WriteLine(callbackBody);
Console.WriteLine();
Console.WriteLine("Redirect the browser back to:");
Console.WriteLine(sender.BuildReturnRedirectUrl(options));

if (!callbackResponse.IsSuccessStatusCode)
{
    Console.Error.WriteLine("The callback request failed.");
    return 1;
}

try
{
    using var document = JsonDocument.Parse(callbackBody);
    Console.WriteLine();
    Console.WriteLine("Parsed callback response:");
    Console.WriteLine(JsonSerializer.Serialize(document, new JsonSerializerOptions
    {
        WriteIndented = true
    }));
}
catch
{
    // Keep the raw body above when the response is not JSON.
}

return 0;

static bool HasPlaceholderConfiguration(PortalSsoSenderOptions options)
{
    return string.IsNullOrWhiteSpace(options.CallbackUrl) ||
           options.CallbackUrl.Contains("example.com", StringComparison.OrdinalIgnoreCase) ||
           string.IsNullOrWhiteSpace(options.ReturnUrl) ||
           options.ReturnUrl.Contains("example.com", StringComparison.OrdinalIgnoreCase) ||
           string.IsNullOrWhiteSpace(options.State) ||
           options.State.Contains("copy-from-sso-start", StringComparison.OrdinalIgnoreCase) ||
           string.IsNullOrWhiteSpace(options.Nonce) ||
           options.Nonce.Contains("copy-from-sso-start", StringComparison.OrdinalIgnoreCase) ||
           string.IsNullOrWhiteSpace(options.ExchangeToken) ||
           options.ExchangeToken.Contains("portal-one-time-token", StringComparison.OrdinalIgnoreCase) ||
           string.IsNullOrWhiteSpace(options.PortalSigningPrivateKeyPem) ||
           LooksLikePem(options.PortalSigningPrivateKeyPem) == false ||
           string.IsNullOrWhiteSpace(options.AuthEncryptionPublicKeyPem) ||
           LooksLikePem(options.AuthEncryptionPublicKeyPem) == false;
}

static Dictionary<string, string?> ReadEnvironmentOverrides()
{
    var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    foreach (var property in typeof(PortalSsoSenderOptions).GetProperties())
    {
        var singleUnderscore = Environment.GetEnvironmentVariable($"PORTAL_SSO_{property.Name}");
        var doubleUnderscore = Environment.GetEnvironmentVariable($"PORTAL_SSO__{property.Name}");
        var value = !string.IsNullOrWhiteSpace(singleUnderscore)
            ? singleUnderscore
            : doubleUnderscore;

        if (!string.IsNullOrWhiteSpace(value))
            overrides[property.Name] = value;
    }

    return overrides;
}

static void HydrateKeyMaterial(PortalSsoSenderOptions options)
{
    if (!string.IsNullOrWhiteSpace(options.PortalSigningPrivateKeyPath))
        options.PortalSigningPrivateKeyPem = File.ReadAllText(options.PortalSigningPrivateKeyPath);

    if (!string.IsNullOrWhiteSpace(options.AuthEncryptionPublicKeyPath))
        options.AuthEncryptionPublicKeyPem = File.ReadAllText(options.AuthEncryptionPublicKeyPath);
}

static bool LooksLikePem(string value)
{
    return value.Contains("-----BEGIN ", StringComparison.OrdinalIgnoreCase) &&
           value.Contains("-----END ", StringComparison.OrdinalIgnoreCase);
}

static string? GetExplicitConfigPath(string[] args)
{
    for (var index = 0; index < args.Length; index++)
    {
        if (!string.Equals(args[index], "--config", StringComparison.OrdinalIgnoreCase))
            continue;

        if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
            throw new InvalidOperationException("The --config option requires a file path.");

        return Path.GetFullPath(args[index + 1]);
    }

    return null;
}
