using System.Security.Cryptography;
using System.Net.Http.Json;
using Jose;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace PortalSsoSender;

public class PortalSsoSender
{
    public string BuildEncryptedPayload(PortalSsoSenderOptions options)
    {
        Validate(options);

        var now = DateTime.UtcNow;
        var claims = new Dictionary<string, object?>
        {
            [JwtRegisteredClaimNames.Iss] = options.Issuer,
            [JwtRegisteredClaimNames.Aud] = options.Audience,
            [JwtRegisteredClaimNames.Iat] = EpochTime.GetIntDate(now),
            [JwtRegisteredClaimNames.Nbf] = EpochTime.GetIntDate(now),
            [JwtRegisteredClaimNames.Exp] = EpochTime.GetIntDate(now.AddMinutes(options.LifetimeMinutes)),
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
            [JwtRegisteredClaimNames.Sub] = options.Subject ?? options.UserName ?? options.Email ?? options.SourceSystemId,
            ["state"] = options.State,
            ["nonce"] = options.Nonce,
            ["source_system"] = options.SourceSystemId,
            ["source_url"] = options.SourceUrl,
            ["exchange_token"] = options.ExchangeToken,
            ["preferred_username"] = options.UserName,
            ["email"] = options.Email
        };

        var compactClaims = claims
            .Where(entry => entry.Value is not null && !string.IsNullOrWhiteSpace(entry.Value.ToString()))
            .ToDictionary(entry => entry.Key, entry => entry.Value!);

        using var signingRsa = RSA.Create();
        signingRsa.ImportFromPem(options.PortalSigningPrivateKeyPem);

        using var authEncryptionRsa = RSA.Create();
        authEncryptionRsa.ImportFromPem(options.AuthEncryptionPublicKeyPem);

        var signedPayload = JWT.Encode(compactClaims, signingRsa, JwsAlgorithm.PS256);
        return JWT.Encode(signedPayload, authEncryptionRsa, JweAlgorithm.RSA_OAEP_256, JweEncryption.A256GCM);
    }

    public async Task<HttpResponseMessage> SendCallbackAsync(PortalSsoSenderOptions options, CancellationToken cancellationToken = default)
    {
        var encryptedPayload = BuildEncryptedPayload(options);
        return await SendCallbackAsync(options, encryptedPayload, cancellationToken);
    }

    public async Task<HttpResponseMessage> SendCallbackAsync(
        PortalSsoSenderOptions options,
        string encryptedPayload,
        CancellationToken cancellationToken = default)
    {
        Validate(options);

        using var client = new HttpClient();
        return await client.PostAsJsonAsync(
            options.CallbackUrl,
            new
            {
                state = options.State,
                encryptedPayload
            },
            cancellationToken);
    }

    public string BuildReturnRedirectUrl(PortalSsoSenderOptions options)
    {
        Validate(options);
        return AppendQueryString(options.ReturnUrl, new Dictionary<string, string?>
        {
            ["state"] = options.State,
            ["sso"] = "1"
        });
    }

    private static void Validate(PortalSsoSenderOptions options)
    {
        var requiredFields = new Dictionary<string, string?>
        {
            [nameof(options.CallbackUrl)] = options.CallbackUrl,
            [nameof(options.ReturnUrl)] = options.ReturnUrl,
            [nameof(options.Issuer)] = options.Issuer,
            [nameof(options.Audience)] = options.Audience,
            [nameof(options.SourceSystemId)] = options.SourceSystemId,
            [nameof(options.SourceUrl)] = options.SourceUrl,
            [nameof(options.State)] = options.State,
            [nameof(options.Nonce)] = options.Nonce,
            [nameof(options.ExchangeToken)] = options.ExchangeToken,
            [nameof(options.PortalSigningPrivateKeyPem)] = options.PortalSigningPrivateKeyPem,
            [nameof(options.AuthEncryptionPublicKeyPem)] = options.AuthEncryptionPublicKeyPem
        };

        var missing = requiredFields
            .Where(entry => string.IsNullOrWhiteSpace(entry.Value))
            .Select(entry => entry.Key)
            .ToList();

        if (missing.Count > 0)
            throw new InvalidOperationException($"Missing required sender options: {string.Join(", ", missing)}");
    }

    private static string AppendQueryString(string url, IReadOnlyDictionary<string, string?> queryValues)
    {
        var separator = url.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var query = string.Join(
            "&",
            queryValues
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Value))
                .Select(entry => $"{Uri.EscapeDataString(entry.Key)}={Uri.EscapeDataString(entry.Value!)}"));

        return string.IsNullOrWhiteSpace(query)
            ? url
            : $"{url}{separator}{query}";
    }
}
