# Authentication

> **Status:** `core`
> **Removable in derived repos:** **no** — every staff app needs login
> **Required by:** every authenticated controller, every authenticated FE page, `authorization-access-functions`, `audit-logging` (login/logout entries), `caching-valkey` (session store)

The Authentication feature owns the entire login surface: the dedicated `Auth` API microservice (port 5001), the Vue auth shell at `src/frontend/auth/`, the Valkey-backed session store, the `X-Session-Id` header convention, and the dual-path login model — direct **NIE IDP** username/password and **Portal SSO** (encrypted JWE callback exchange). The Main API never authenticates a user; it only consumes a session token via `SessionValidationMiddleware` and looks the session up in Valkey. This split keeps the auth surface tight: one Sentry-instrumented service, one place to rotate keys, one place to audit login events.

Sessions are JSON-serialized `AuthSessionDto` blobs stored under the key `session:{sessionToken}` in Valkey with a sliding-window expiry of `ValidSessionTimeInMins` minutes. The Auth API issues; the Main API validates. Roles and access functions are NOT in the session payload — they are fetched separately by the FE from the Main API after redirect (see `authorization-access-functions`).

## Quick links

- [`files.md`](./files.md) — every file owned and touched by this feature
- [`do-dont.md`](./do-dont.md) — feature-specific rules
- [`customize.md`](./customize.md) — common login-flow customizations
- [`verify.md`](./verify.md) — proof the auth flow works end to end

## Architectural shape

```mermaid
flowchart LR
  Browser["Browser<br/>(auth FE :8002)"] -->|POST Login| AuthApi["Auth API :5001<br/>AuthController"]
  Browser -->|GET SsoStart| AuthApi
  PortalSso["NIE Portal<br/>(JWE callback)"] -->|POST SsoCallback| AuthApi
  AuthApi -->|verify creds| IDP["NIE IDP<br/>NIEAuthApi"]
  AuthApi -->|set session:{token}| Valkey[(Valkey)]
  Browser -->|redirect with SessionToken cookie| MainFe["Main FE :8001"]
  MainFe -->|every request<br/>X-Session-Id header| MainApi["Main API :5002<br/>SessionValidationMiddleware"]
  MainApi -->|GET session:{token}| Valkey
  MainApi -->|access function lookup| Db[(MainDbContext)]
```

## Key entry points

| Layer | Path | Purpose |
| --- | --- | --- |
| Auth API host | `src/backend/Auth/Program.cs` | Boots the Auth service, wires Valkey, CORS, Swagger, Sentry+OTel |
| Auth API controller | `src/backend/Auth/Controllers/AuthController.cs` | `Login`, `SsoStart`, `SsoCallback`, `SsoFinalize`, `Refresh`, `Verify`, `GetProfile`, `CreateTestSession` |
| Session issuer | `src/backend/Auth/Services/AuthSessionService.cs` | `IssueSessionAsync` — JSON-serializes `AuthSessionDto` and writes `session:{token}` |
| Portal SSO worker | `src/backend/Auth/Services/PortalSsoService.cs` | JWE decryption, JWS verification, replay detection (`sso:jti:`), state record (`sso:state:`), exchange-token POST |
| Main API session check | `src/backend/API/Middleware/SessionValidationMiddleware.cs` | Reads `X-Session-Id`, looks up `session:{token}` in Valkey, populates `HttpContext.Items` |
| User context | `src/backend/Libraries/Services/Services/UserContextService.cs` | Wraps `HttpContext.Items` keys (`KeySessionUserId`, `KeySessionUserName`, etc.) |
| FE login page | `src/frontend/auth/src/components/LoginPage.vue` | Username/password form, SSO launch button |
| FE auth service | `src/frontend/main/src/services/authService.ts` | `ensureAuthenticated`, `redirectToLogin`, `getAuthLoginUrl` (cookie-driven gate) |
| FE auth composable | `src/frontend/main/src/composables/useAuth.ts` | `currentUser`, `isAuthenticated`, `logout`, `hasRole` reactive state |
| Session DTO | `src/backend/Auth/Models/AuthSessionDto.cs` | Shape of the JSON in Valkey: `UserId`, `Name`, `Email`, `Department`, `LastActive` |
| SSO config | `src/backend/Auth/Models/PortalSsoOptions.cs` | `Crypto`, `LaunchUrlTemplate`, `ExchangeApi`, `AllowedIpRanges`, `AllowedSourceUrls` |
