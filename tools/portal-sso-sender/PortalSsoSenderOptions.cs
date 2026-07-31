namespace PortalSsoSender;

public class PortalSsoSenderOptions
{
    public string CallbackUrl { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SourceSystemId { get; set; } = "portal-app";
    public string SourceUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string ExchangeToken { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Subject { get; set; }
    public int LifetimeMinutes { get; set; } = 5;
    public string PortalSigningPrivateKeyPath { get; set; } = string.Empty;
    public string PortalSigningPrivateKeyPem { get; set; } = string.Empty;
    public string AuthEncryptionPublicKeyPath { get; set; } = string.Empty;
    public string AuthEncryptionPublicKeyPem { get; set; } = string.Empty;
}
