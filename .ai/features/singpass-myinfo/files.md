# Singpass MyInfo — File Map

## Owned files

### Backend

| Path | Layer | Purpose |
| --- | --- | --- |
| `src/backend/API/Controllers/MyInfoController.cs` | Controller | `GET GetAuthorizeUrl`, `POST Callback`, `GET IsConfigured`. State / nonce / code-verifier / DPoP key lifecycle in Valkey under `myinfo:state:{stateId}` with 10-minute TTL. Returns `400` when `_myInfoService.IsConfigured` is false. |
| `src/backend/Libraries/Services/Services/MyInfo/IMyInfoService.cs` | Interface | `IsConfigured`, `CreateAuthorizationRequestAsync(state)`, `GetPersonDataAsync(authCode, codeVerifier, nonce, dpopPrivateKey)` |
| `src/backend/Libraries/Services/Services/MyInfo/MyInfoService.cs` | Service | The full FAPI implementation: discovery doc, JWS client_assertion, PAR, DPoP proof, JWE decryption (library path + manual ECDH-ES + AES-KW + AES-GCM/CBC-HMAC fallback), UserInfo parsing into `MyInfoPersonData`. ~1240 lines. |
| `src/backend/API/MyInfo/Jwks/private-jwks.json` | Key material | The client's private JWKS (one signing EC key, one encryption EC key). MUST be replaced per environment; the file in the template is a placeholder for shape. |

### Frontend

| Path | Layer | Purpose |
| --- | --- | --- |
| `src/frontend/main/src/staff/pages/staff/MyInfoPage.vue` | Page | Entry point — calls `GET /GetAuthorizeUrl`, redirects browser to Singpass, then renders the returned person data when `MyInfoCallback.vue` finishes |
| `src/frontend/main/src/staff/pages/staff/MyInfoCallback.vue` | Page | Catches the `?code=&state=` redirect, posts to `/Callback`, navigates back to `MyInfoPage` with state |
| `src/frontend/main/src/services/myInfoService.ts` | Service | Typed axios client for `getAuthorizeUrl`, `submitCallback`, `isConfigured` |

### Models / DTOs (defined inline)

| Path | Layer | Purpose |
| --- | --- | --- |
| `MyInfoController.cs` (record types) | DTO | `MyInfoCallbackRequest(string AuthCode, string State)`, `MyInfoAuthSessionState(StateId, CodeVerifier, Nonce, DpopPrivateKey, IssuedAtUtc)` |
| `MyInfoService.cs` (nested records) | DTO | `MyInfoDiscoveryDocument`, `StoredEcJwk`, `EphemeralEcJwk`, `TokenExchangeResult`, `ValidatedJwtResult` |
| `MyInfoPersonData` (separate file under Domain) | DTO | The flattened response: `Name`, `NricFin`, `Sex`, `Race`, `Nationality`, `DateOfBirth`, `BirthCountry`, `ResidentialStatus`, `MaritalStatus`, `Email`, `MobileNumber`, `PostalCode`, `BlockNumber`, `StreetName`, `FloorNumber`, `UnitNumber`, `RegisteredAddress`, `HighestQualification`, `Occupation`, `EmployerName`, `Subject`, `VerifiedAtUtc` |

## Touched files

| Path | What it contains | Why must be touched |
| --- | --- | --- |
| `src/backend/API/Program.cs` | `builder.Services.AddMemoryCache()` (line 109), `builder.Services.AddHttpClient<IMyInfoService, MyInfoService>()` (line 110) | Required for the `IMemoryCache` used by discovery / signing-key / client-key caches inside the service, and for the typed `HttpClient` |
| `src/backend/API/appsettings.json` | `MyInfo` section: `ClientId`, `RedirectUri`, `DiscoveryUrl` (optional, falls back to `BaseUrl + /fapi/.well-known/openid-configuration`), `BaseUrl` (default `https://stg-id.singpass.gov.sg`), `Scopes` (or `Attributes`), `JwtClientAuthentication.PrivateJwksPath`, `JwtClientAuthentication.SigningKeyId`, `JwtClientAuthentication.EncryptionKeyId` | Without these the service initializes with empty fields and `IsConfigured` returns false; controller short-circuits to `400` |
| `src/backend/API/Controllers/MyInfoController.cs` | Uses `IDistributedCache` from `caching-valkey` | Co-feature dependency on Valkey for state storage |
| `src/backend/Libraries/Domain/Models/MyInfoPersonData.cs` (or wherever defined) | Shared DTO returned to FE | Schema must stay aligned with FE consumer types in `myInfoService.ts` |
| `src/frontend/main/src/router/index.ts` | Routes `/myinfo` and `/myinfo/callback` | Required so the redirect from Singpass lands on the callback page |
| `src/frontend/main/src/services/myInfoService.ts` | Hardcoded API paths `/api/MyInfo/GetAuthorizeUrl` and `/api/MyInfo/Callback` | Match the controller routes; touch when controller routes are renamed |

## Migrations

None — MyInfo writes nothing to PostgreSQL. State lives in Valkey, person data is returned to the FE and stored only in volatile memory of the page component (or wherever the project chooses to persist it — that's a project decision).

## External dependencies

| Package | Purpose |
| --- | --- |
| `Microsoft.IdentityModel.JsonWebTokens` | Token validation (`JsonWebTokenHandler`) |
| `Microsoft.IdentityModel.Tokens` | `TokenValidationParameters`, `ECDsaSecurityKey`, `JsonWebKey`, `Base64UrlEncoder` |
| `System.IdentityModel.Tokens.Jwt` | `JwtSecurityToken`, `JwtSecurityTokenHandler`, `JwtHeader`, `JwtPayload` for client assertion creation |
| `System.Security.Cryptography` | `ECDsa`, `ECDiffieHellman`, `AesGcm`, manual JWE decryption |
| `Microsoft.Extensions.Caching.Memory` | In-process cache for discovery doc, issuer signing keys, client keys |
| `Microsoft.Extensions.Caching.Distributed` (Valkey) | State / DPoP key store across requests |

## Removal note

This feature is `optional-core`. A future `.ai/tasks/NNNN-remove-singpass-myinfo` task will:

1. Delete `MyInfoController.cs`, `IMyInfoService.cs`, `MyInfoService.cs`, `private-jwks.json`.
2. Delete `MyInfoPage.vue`, `MyInfoCallback.vue`, `myInfoService.ts`.
3. Remove the `MyInfo` section from `appsettings.json`.
4. Remove `AddHttpClient<IMyInfoService, MyInfoService>` from `Program.cs`.
5. Remove the routes in `router/index.ts`.
6. Leave `IMemoryCache` registration (it is harmless and may be used by other features).
