# Authentication — Do and Don't

## DO ✅

1. **DO** read the session via `IUserContextService` (or `HttpContext.Items[Constants.KeySessionUserId]`) — never read the `X-Session-Id` header directly in a controller or service. The middleware has already validated and unpacked it.
2. **DO** use the `[AllowAnonymous]` attribute on any endpoint that must skip session validation. `SessionValidationMiddleware.InvokeAsync` checks for `endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>()` and bypasses Valkey lookup. The skip-by-path list (`/swagger`, `/health`, `/favicon.ico`, `/tickerq`) is the only other escape hatch.
3. **DO** keep the Auth API stateless against Postgres — it must remain a Valkey + IDP-only service. Adding a `DbContext` to `Auth.csproj` violates the architecture and breaks the deployment isolation. Role/permission resolution belongs in the Main API.
4. **DO** write to `session:{token}` ONLY through `AuthSessionService.IssueSessionAsync`. That keeps the cache-entry shape, expiry, and key prefix in one place.
5. **DO** use `DateTimeHelper.Now` (Singapore wall-clock) for `LastActive`. The middleware normalizes UTC payloads via `NormalizeLastActiveToSingapore` for backwards compatibility, but new writes must be Singapore-local.
6. **DO** validate the SSO state, nonce, source-system, source-URL, JWE alg, JWS alg, and `jti` replay — every check inside `PortalSsoService.ValidatePayloadAsync` exists for a reason. Removing one removes a defense layer.
7. **DO** pin the JWE outer `alg` and `enc` and the inner JWS `alg` via config (`PortalSso:Crypto:RequiredOuterAlg`, `RequiredEnc`, `RequiredInnerAlg`). Never accept tokens with algorithms the partner system did not pre-register.
8. **DO** keep dev-only test sessions guarded behind `_environment.IsDevelopment()` in `AuthController.CreateTestSession`. Removing the guard exposes a session-minting endpoint in production.
9. **DO** redirect through `authService.redirectToLogin()` (FE) on every 401, so the cookie is cleared before the browser re-enters the login flow. Manual `window.location.href = "/"` skips that cleanup.
10. **DO** scope `KeySession*` constants in `Shared.Globals.Constants` — that is the contract between middleware and consumers.

## DON'T ❌

1. **DON'T** add database access to the Auth API. The split is intentional: Auth handles credentials and sessions, Main API handles authorization. Mixing them recreates the monolith we deliberately broke up.
2. **DON'T** put roles, permissions, or access functions inside the Valkey session blob. They are fetched by the FE from the Main API (`AccessControlController.GetCurrentAccessProfile`). Putting them in the session means a stale session keeps stale roles after a role change.
3. **DON'T** read `Request.Cookies["SessionToken"]` from a controller. The cookie is FE-only; the Main API contract is the `X-Session-Id` header. The cookie path was deliberately commented out in `SessionValidationMiddleware.GetSessionId`.
4. **DON'T** call `IDistributedCache.SetStringAsync("session:..."...)` from anywhere outside `AuthSessionService` — duplicating the write pattern means future expiry/logging changes won't reach all writers.
5. **DON'T** trust `IsAuthenticated` claims-based identity from ASP.NET — the project uses session-based auth, not JWT bearer. `[Authorize]` without `[AllowAnonymous]` will not behave the way you expect; use `[RequireAccessFunction(...)]` instead.
6. **DON'T** add a new SSO source by adding an `if (sourceSystemId == "newPartner")` branch — extend `PortalSsoOptions` (one config block per source) and let the existing `ValidatePayloadAsync` loop handle it.
7. **DON'T** weaken `RequireSignedTokens = true` or `RequireExpirationTime = true` on `TokenValidationParameters` to "make local testing easier". Use `CreateTestSession` for local testing instead.
8. **DON'T** log the full session token value. Log `userId` / `correlationId` / a hash if you must — leaking the token in app logs is equivalent to leaking a password.
9. **DON'T** hardcode `session:` cache prefix in new code — that string lives in `AuthSessionService`, `SessionValidationMiddleware`, and `AuthController.Refresh`. If you change the prefix, change all three (and consider promoting it to a `Constants.SessionCachePrefix`).
10. **DON'T** rely on `Verify(GET)` (header-driven) for every request — that endpoint is a debug helper. Per-request validation is automatic via `SessionValidationMiddleware`. Calling `/api/Auth/Verify` from a hot path adds latency.
