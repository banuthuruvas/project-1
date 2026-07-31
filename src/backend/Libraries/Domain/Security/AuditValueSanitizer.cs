using System.Security.Cryptography;
using System.Text;

namespace Domain.Security;

/// <summary>
/// Normalizes request metadata before it is persisted in the audit log.
/// Session credentials are represented by a stable, non-reversible fingerprint
/// so an upstream token of arbitrary length is never stored in the database.
/// </summary>
public static class AuditValueSanitizer
{
    public const int EntityNameMaxLength = 256;
    public const int EntityIdMaxLength = 256;
    public const int UserIdMaxLength = 256;
    public const int UserNameMaxLength = 256;
    public const int IpAddressMaxLength = 50;
    public const int UserAgentMaxLength = 512;
    public const int CorrelationIdMaxLength = 256;
    public const int SessionIdMaxLength = 256;
    public const int RequestMethodMaxLength = 10;
    public const int RequestUrlMaxLength = 2048;
    public const int OutcomeMaxLength = 20;

    public static string? Limit(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value.Length <= maxLength ? value : value[..maxLength];
    }

    public static string? FingerprintSessionId(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return null;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sessionId));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
