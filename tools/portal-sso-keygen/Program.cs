using System.Security.Cryptography;

var outputDir = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "tools", "portal-sso-sender", ".dev-keys");

var fullOutputDir = Path.GetFullPath(outputDir);
Directory.CreateDirectory(fullOutputDir);

WriteKeyPair(
    Path.Combine(fullOutputDir, "portal-signing-private.pem"),
    Path.Combine(fullOutputDir, "portal-signing-public.pem"));

WriteKeyPair(
    Path.Combine(fullOutputDir, "auth-decryption-private.pem"),
    Path.Combine(fullOutputDir, "auth-decryption-public.pem"));

Console.WriteLine($"Generated development keys in {fullOutputDir}");

static void WriteKeyPair(string privateKeyPath, string publicKeyPath)
{
    using var rsa = RSA.Create(3072);

    File.WriteAllText(privateKeyPath, rsa.ExportRSAPrivateKeyPem());
    File.WriteAllText(publicKeyPath, rsa.ExportRSAPublicKeyPem());
}
