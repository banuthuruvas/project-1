# Task 0006 — SSRF Outbound Allowlist

> **Status:** scaffolded.
> **Why:** OWASP W-A10 / API7 — `MyInfoService` and `PortalSsoService` make outbound HTTP using URLs from configuration. If config gets compromised (or a developer points a staging build at an attacker URL), the API would happily proxy. This task fences each external integration behind an explicit allowlist.

## Pre-checks

```bash
test -f src/backend/Libraries/Shared/Helpers/SsrfGuard.cs && { echo "Already applied."; exit 0; }
```

## 1. Files to create

### `src/backend/Libraries/Shared/Helpers/SsrfGuard.cs`

```csharp
namespace Shared.Helpers;

public static class SsrfGuard
{
    public static Uri Validate(string url, IReadOnlyCollection<string> allowedHosts, string contextLabel)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException($"{contextLabel}: URL is empty.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"{contextLabel}: '{url}' is not an absolute URL.");

        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException($"{contextLabel}: '{url}' must be HTTPS.");

        var host = uri.Host;
        if (!allowedHosts.Any(allowed => HostMatches(host, allowed)))
            throw new InvalidOperationException(
                $"{contextLabel}: host '{host}' is not in the allowlist [{string.Join(", ", allowedHosts)}].");

        return uri;
    }

    private static bool HostMatches(string host, string pattern)
    {
        // Pattern can be exact ("api.myinfo.gov.sg") or wildcard ("*.gov.sg").
        if (pattern.StartsWith("*."))
            return host.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase);
        return string.Equals(host, pattern, StringComparison.OrdinalIgnoreCase);
    }
}
```

### `tests/specs/api/ssrf-guard.spec.ts`

A Playwright test that hits a synthetic endpoint configured to call a forbidden host and asserts a 5xx with the SSRF rejection in the body.

## 2. Files to edit

### `src/backend/Libraries/Services/Services/MyInfo/MyInfoService.cs`

Before every outbound call (e.g. token endpoint, person endpoint), call:

```csharp
var validated = SsrfGuard.Validate(
    _settings.PersonApiBaseUrl,
    _settings.AllowedHosts,
    "MyInfo Person API");
```

Add `AllowedHosts` (`List<string>`) to `MyInfoSettings` if it doesn't exist. Default value in `appsettings.json`:

```json
{
  "MyInfo": {
    "AllowedHosts": ["test.api.myinfo.gov.sg", "api.myinfo.gov.sg"]
  }
}
```

### `src/backend/Auth/Services/PortalSsoService.cs`

Same pattern for portal IDP base URL + redirect endpoints. Source allowlist from `PortalSsoOptions.AllowedHosts`.

## 3. Verification

```bash
dotnet build src/backend/NieTemplate.sln
grep -rn "SsrfGuard\.Validate" src/backend/Libraries/Services/Services/MyInfo/ src/backend/Auth/Services/ | wc -l   # ≥ 2
```

Manually point `MyInfo:PersonApiBaseUrl` at `https://example.com` in `appsettings.Development.json` and call the endpoint — expect HTTP 500 with the allowlist message.

## 4. Rollback

```bash
git restore --staged --worktree src/backend/Libraries/Shared/Helpers/SsrfGuard.cs src/backend/Libraries/Services/Services/MyInfo/ src/backend/Auth/Services/
```

## Maintainer review

- [ ] Confirm production allowlist values (Singpass test vs prod)
- [ ] Audit any other `HttpClient` / `IHttpClientFactory` usage for the same pattern (Sentry, OneSignal — both use vendor SDKs but verify)
