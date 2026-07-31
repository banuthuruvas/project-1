# Authentication — File Map

## Owned files

### Auth API (microservice)

| Path | Layer | Purpose |
| --- | --- | --- |
| `src/backend/Auth/Auth.csproj` | Project | Auth service project file (no DbContext reference — it's IDP + Valkey only) |
| `src/backend/Auth/Program.cs` | Host | Builds the Auth WebApplication: Valkey `IConnectionMultiplexer`, `AddStackExchangeRedisCache`, CORS, Swagger, Sentry+OpenTelemetry |
| `src/backend/Auth/Controllers/AuthController.cs` | Controller | All public endpoints: `Login`, `SsoStart`, `SsoCallback`, `SsoFinalize`, `Refresh`, `Verify`, `GetProfile`, `CreateTestSession` |
| `src/backend/Auth/Services/AuthSessionService.cs` | Service | `IssueSessionAsync` — single source of truth for writing `session:{token}` in Valkey |
| `src/backend/Auth/Services/PortalSsoService.cs` | Service | Portal SSO state machine: `StartAsync`, `HandleCallbackAsync`, `FinalizeAsync`. JWE/JWS validation, replay detection, exchange-token call |
| `src/backend/Auth/appsettings.json` | Config | `NIEAuthApi`, `Valkey`, `PortalSso`, `ValidSessionTimeInMins`, `AllowedCORSOrigin`, `Sentry` |

### Auth API DTOs and models

| Path | Layer | Purpose |
| --- | --- | --- |
| `src/backend/Auth/Models/AuthSessionDto.cs` | DTO | Shape of the value at `session:{token}` in Valkey |
| `src/backend/Auth/Models/LoginRequest.cs` | DTO | `{ userid, pd }` body for `POST /api/Auth/Login` |
| `src/backend/Auth/Models/LoginResponse.cs` | DTO | NIE IDP response shape (`isAuthenticated`, `userId`, `fullName`, `email`, `department`, `sessionToken`) |
| `src/backend/Auth/Models/IssuedLoginResponse.cs` | DTO | What the Auth API returns to the FE after issuing the Valkey session |
| `src/backend/Auth/Models/RefreshResponse.cs` | DTO | Wrapper for IDP refresh result (`RefreshResponseRoot.result.authenticated`) |
| `src/backend/Auth/Models/CreateTestSessionRequest.cs` | DTO | Dev-only request body for `CreateTestSession` |
| `src/backend/Auth/Models/CreateTestSessionResponse.cs` | DTO | Dev-only response carrying the freshly issued sessionToken |
| `src/backend/Auth/Models/VerifyResponse.cs` | DTO | IDP verify response (success boolean) |
| `src/backend/Auth/Models/ProfileResponse.cs` | DTO | Trimmed profile shape returned by `GetProfile` |
| `src/backend/Auth/Models/PortalSsoOptions.cs` | Options | `PortalSso:*` config: `Enabled`, `LaunchUrlTemplate`, `Crypto.DecryptionPrivateKeyPem/Path`, `Crypto.SigningPublicKeyPem/Path`, `Crypto.RequiredOuterAlg`, `Crypto.RequiredEnc`, `Crypto.RequiredInnerAlg`, `Issuer`, `Audience`, `SourceSystemId`, `SourceSystemClaim`, `SourceUrlClaim`, `ExchangeTokenClaim`, `EmailClaim`, `UsernameClaim`, `DefaultReturnUrl`, `CallbackUrl`, `AllowedIpRanges`, `AllowedSourceUrls`, `StateTtlMinutes`, `ReplayTtlMinutes`, `FinalizePollIntervalMs`, `ExchangeApi.BaseUrl/Path/SubscriptionHeaderName/SubscriptionKey/SourceHeaderName` |
| `src/backend/Auth/Models/SsoStateRecord.cs` | Model | Status (`Pending`, `Completed`, `Failed`), `Nonce`, `ReturnUrl`, `Login`, `ErrorMessage`, `CreatedAt`, `CompletedAt` |
| `src/backend/Auth/Models/SsoCallbackRequest.cs` | DTO | `{ state, encryptedPayload }` |
| `src/backend/Auth/Models/SsoStartResponse.cs` | DTO | `state, nonce, launchUrl, pollIntervalMs` |
| `src/backend/Auth/Models/SsoFinalizeResult.cs` | DTO | `status`, `login`, `message`, `pollIntervalMs` |
| `src/backend/Auth/Models/SsoValidatedPayload.cs` | Model | Internal struct for inner JWS claims |

### Main API session enforcement

| Path | Layer | Purpose |
| --- | --- | --- |
| `src/backend/API/Middleware/SessionValidationMiddleware.cs` | Middleware | The only consumer of session state in the Main API. Reads `X-Session-Id`, validates against Valkey, populates `HttpContext.Items[Constants.KeySession*]` |
| `src/backend/API/Middleware/UserRolesMiddleware.cs` | Middleware | Hydrates `KeySessionUserAccessFunctions` after `SessionValidationMiddleware` (used by `[RequireAccessFunction]`) |
| `src/backend/Libraries/Services/Services/UserContextService.cs` | Service | Adapts `HttpContext.Items` to a typed `IUserContextService.UserId/UserName/Email/SessionId` for use in services |
| `src/backend/Libraries/Shared/Globals/Constants.cs` | Constants | `KeySessionUserId`, `KeySessionUserName`, `KeySessionUserEmail`, `KeySessionSessionId`, `KeySessionUserDept`, `KeySessionUserAccessFunctions` — keys used in `HttpContext.Items` |

### Frontend

| Path | Layer | Purpose |
| --- | --- | --- |
| `src/frontend/auth/src/App.vue` | Page shell | Auth FE root |
| `src/frontend/auth/src/components/LoginPage.vue` | Page | Username/password form, SSO launch button, redirect logic |
| `src/frontend/auth/src/main.ts` | Bootstrap | Mounts the auth FE app |
| `src/frontend/main/src/services/authService.ts` | Service | `ensureAuthenticated()`, `redirectToLogin()`, `getAuthLoginUrl()` — cookie-driven session gate |
| `src/frontend/main/src/composables/useAuth.ts` | Composable | Reactive `currentUser`, `isAuthenticated`, `isAdmin`, `logout`, `hasRole` |
| `src/frontend/main/src/services/api.ts` | HTTP | Axios instance that injects the `X-Session-Id` header from cookie on every Main API call and redirects to login on 401 |

## Touched files (line-level edits required when changing the auth flow)

| Path | What it contains | Why must be touched |
| --- | --- | --- |
| `src/backend/API/Program.cs` | `builder.Services.AddSessionValidation(configuration)` and `app.UseSessionValidation()` registration | Required so the middleware actually runs in the Main API pipeline |
| `src/backend/API/Middleware/MiddlewareExtensions.cs` | Extension methods registering `SessionValidationMiddleware` and `UserRolesMiddleware` | Both extensions must call `app.UseMiddleware<...>` in the right order |
| `src/backend/API/appsettings.json` | `Valkey:ConnectionString`, `ValidSessionTimeInMins`, `AllowedCORSOrigin` | Main API consumes the same Valkey instance the Auth API writes to; must match |
| `src/backend/Libraries/Domain/Dto/AuthDto.cs` | DTO used by `SessionValidationMiddleware` to deserialize the session blob | Must stay schema-compatible with `Auth.Models.AuthSessionDto` (field-by-field) |
| `src/frontend/main/src/services/api.ts` | Adds `X-Session-Id` header from cookie, intercepts 401 responses | Must call `authService.redirectToLogin()` on 401 |
| `src/frontend/main/src/router/index.ts` | Per-route navigation guard calling `authService.ensureAuthenticated()` | Without this, deep links bypass the cookie check |
| `src/frontend/main/src/main.ts` | Bootstraps the FE and reads `VITE_AUTH_SERVICE_URL` for redirect | URL must point at the Auth FE host |
| `src/frontend/auth/.env` / `src/frontend/main/.env` | `VITE_AUTH_SERVICE_URL`, `VITE_API_BASE_URL`, `VITE_COOKIE_DOMAIN`, `VITE_COOKIE_SESSION_KEY`, `VITE_COOKIE_USER_KEY` | Both FE apps share cookie domain so the Main FE can read the session cookie set by Auth FE |

## Migrations

None — sessions live in Valkey, not in PostgreSQL. There are no auth-owned tables.

## External dependencies

| Package | Project | Purpose |
| --- | --- | --- |
| `StackExchange.Redis` | Auth + API | Valkey client for `IConnectionMultiplexer` and `IDistributedCache` |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | Auth + API | `AddStackExchangeRedisCache(options)` |
| `jose-jwt` (`Jose`) | Auth | JWE decryption in `PortalSsoService` |
| `Microsoft.IdentityModel.JsonWebTokens` | Auth | JWS validation (`JsonWebTokenHandler`) |
| `Microsoft.IdentityModel.Tokens` | Auth | `RsaSecurityKey`, `TokenValidationParameters` |
| `Sentry.AspNetCore` + `Sentry.OpenTelemetry` | Auth + API | Error capture and distributed tracing |
| `js-cookie` | FE | Cookie read/write for session and user |
