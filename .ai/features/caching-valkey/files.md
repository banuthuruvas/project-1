# Caching (Valkey) — File Map

## Owned files

There is no dedicated source file for the cache feature itself — it is configured directly in the two `Program.cs` files (Auth + Main). The cache is consumed by every feature that uses `IDistributedCache` or `IConnectionMultiplexer`.

## Touched files

| Path | What it contains | Why must be touched |
| --- | --- | --- |
| `src/backend/API/Program.cs` lines 53-58 | `builder.Services.AddSingleton<IConnectionMultiplexer>(provider => ConnectionMultiplexer.Connect(connectionString))` | Provides direct `IDatabase` / `ISubscriber` access (used by features that need pub/sub or direct commands) |
| `src/backend/API/Program.cs` lines 61-71 | `builder.Services.AddStackExchangeRedisCache(options => { options.ConfigurationOptions = new ConfigurationOptions { EndPoints = { connectionString }, AbortOnConnectFail = false }; options.InstanceName = ...; })` | Provides `IDistributedCache` — the dominant interface used by `AuthSessionService`, `SessionValidationMiddleware`, `AccessFunctionService`, `MyInfoController`, `PortalSsoService` |
| `src/backend/Auth/Program.cs` lines 47-52, 54-64 | Same two registrations in the Auth process (separate process; separate connection multiplexer) | Required because Auth must write the `session:{token}` keys that the Main API reads |
| `src/backend/API/appsettings.json` `Valkey:ConnectionString`, `Valkey:InstanceName` | Connection settings | Both apps MUST point at the same Valkey instance; mismatched URLs produce mysterious "session not found" failures |
| `src/backend/Auth/appsettings.json` `Valkey:ConnectionString`, `Valkey:InstanceName` | Same keys as Main API | Mirror; usually the same value |
| `src/backend/API/Program.cs` line 76 | `services.AddHealthChecks().AddRedis(configuration["Valkey:ConnectionString"]!, name: "valkey")` | Wires Valkey reachability into `/health` |
| `src/backend/Auth/Services/AuthSessionService.cs` | Owns `session:{token}` writes | Single-source write for the canonical session blob |
| `src/backend/API/Middleware/SessionValidationMiddleware.cs` | Reads `session:{token}` | Per-request session validation |
| `src/backend/API/Controllers/MyInfoController.cs` | Owns `myinfo:state:{stateId}` lifecycle | 10-minute TTL state for MyInfo flow |
| `src/backend/Libraries/Services/Services/AccessFunction/AccessFunctionService.cs` | `user_access_functions_{userId}` key | Per-user access function code list with TTL |
| `src/backend/Auth/Services/PortalSsoService.cs` | `sso:state:{state}` and `sso:jti:{jti}` prefixes (`StatePrefix`, `ReplayPrefix` constants) | Portal SSO state machine + replay defense |

## Migrations

None — Valkey is in-memory; nothing in PostgreSQL.

## External dependencies

| Package | Purpose |
| --- | --- |
| `StackExchange.Redis` | The Valkey/Redis client — `IConnectionMultiplexer`, `ConfigurationOptions` |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | The `AddStackExchangeRedisCache` extension that wires `IDistributedCache` |
| `AspNetCore.HealthChecks.Redis` | The `AddRedis(...)` health probe |

## Key prefix registry

| Prefix | Owner | TTL | Purpose |
| --- | --- | --- | --- |
| `session:{sessionToken}` | `AuthSessionService.IssueSessionAsync` | `ValidSessionTimeInMins` (sliding via re-write) | Session payload (`AuthSessionDto`) |
| `sso:state:{state}` | `PortalSsoService` (`StatePrefix`) | `PortalSso:StateTtlMinutes` | Portal SSO state machine record |
| `sso:jti:{jti}` | `PortalSsoService` (`ReplayPrefix`) | `PortalSso:ReplayTtlMinutes` | Replay defense for inner JWS jti |
| `user_access_functions_{userId}` | `AccessFunctionService` | (configured TTL) | Granted code list per user |
| `myinfo:state:{stateId}` | `MyInfoController` | 10 minutes | MyInfo state / DPoP key bundle |
