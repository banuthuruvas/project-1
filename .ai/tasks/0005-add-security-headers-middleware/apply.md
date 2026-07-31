# Task 0005 — Add Security Headers Middleware

> **Status:** scaffolded — design + outline only. Maintainer must finalize CSP per project before promoting to a release.

> **Why:** OWASP W-A05 / API8 — the template ships without centrally configured security headers. Browsers leak metadata, allow framing, lack HSTS preloading. This task adds a single middleware that emits the standard set on every response and aligns nginx so headers are not duplicated.

## Pre-checks

```bash
test ! -f src/backend/API/Middleware/SecurityHeadersMiddleware.cs || { echo "Already added; skipping."; exit 0; }
```

## 1. Files to create

### `src/backend/API/Middleware/SecurityHeadersOptions.cs` (new)

A typed-options class so per-environment overrides flow through `appsettings.*.json:SecurityHeaders`. Keys to support:

- `ContentSecurityPolicy` (string) — default: `default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'`
- `StrictTransportSecurity` (string) — default: `max-age=31536000; includeSubDomains; preload`
- `XFrameOptions` (string) — default: `DENY`
- `XContentTypeOptions` (string) — default: `nosniff`
- `ReferrerPolicy` (string) — default: `strict-origin-when-cross-origin`
- `PermissionsPolicy` (string) — default: `accelerometer=(), camera=(), geolocation=(), microphone=(), payment=()`
- `EnableForSwagger` (bool) — default: `false` (Swagger UI needs `unsafe-inline` for its bundled JS, so a relaxed CSP applies for `/swagger/*`)

### `src/backend/API/Middleware/SecurityHeadersMiddleware.cs` (new)

```csharp
using Microsoft.Extensions.Options;

namespace API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeadersOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        var isSwagger = context.Request.Path.StartsWithSegments("/swagger");

        if (!isSwagger || _options.EnableForSwagger)
        {
            headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
        }
        headers["Strict-Transport-Security"] = _options.StrictTransportSecurity;
        headers["X-Frame-Options"] = _options.XFrameOptions;
        headers["X-Content-Type-Options"] = _options.XContentTypeOptions;
        headers["Referrer-Policy"] = _options.ReferrerPolicy;
        headers["Permissions-Policy"] = _options.PermissionsPolicy;

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
```

## 2. Files to edit

### `src/backend/API/Program.cs`

Register options + middleware. Place `UseSecurityHeaders()` AFTER `UseRateLimiter()` and `UseCors()`, BEFORE `UseSwagger()` and `MapControllers()`.

```diff
+        builder.Services.Configure<SecurityHeadersOptions>(configuration.GetSection("SecurityHeaders"));
         builder.Services.AddControllers();
…
         app.UseCors("AllowSpecificOrigin");
+        app.UseSecurityHeaders();
         app.UseResponseCaching();
```

### `build/nginx.conf`

Audit and remove any duplicate emissions of the same headers. The API is the single source of truth from now on; nginx should only enforce TLS termination + proxying.

## 3. Verification

```bash
dotnet build src/backend/NieTemplate.sln
# Live smoke
curl -sI http://localhost:5002/health | grep -iE 'strict-transport|content-security|x-content-type|x-frame|referrer-policy|permissions-policy' | wc -l   # expect ≥ 6
```

## 4. Rollback

```bash
git restore --staged --worktree src/backend/API/Middleware/Security* src/backend/API/Program.cs build/nginx.conf
```

## Maintainer review checklist before promoting to a release

- [ ] CSP tightened against any third-party origin actually loaded (Sentry CDN, OneSignal, fonts)
- [ ] `EnableForSwagger=true` only in non-prod
- [ ] HSTS `preload` directive validated against your domain (otherwise drop `preload`)
- [ ] Nginx config tested end-to-end with the API to confirm no duplication
- [ ] `templateVersionAfterApply` set; release manifest updated
