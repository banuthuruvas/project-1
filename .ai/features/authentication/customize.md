# Authentication — Customize

This file lists the most common customizations and the exact files/lines to edit. All edits must keep the Auth ↔ Main API split intact; if a change requires the Auth API to read the database, treat that as a redesign, not a customization.

## 1. Change the session sliding-expiry window

1. Edit `src/backend/Auth/appsettings.json` — change `"ValidSessionTimeInMins": 30` to your desired number (e.g. `60`).
2. Edit `src/backend/API/appsettings.json` — change the same key. Both must match because `SessionValidationMiddleware.ValidateSessionAsync` re-checks `lastActiveSgt + ValidSessionTimeInMins` on every request.
3. Restart both services. There is no migration; existing sessions in Valkey simply use the new window on their next request.

## 2. Add a new field to the session payload (e.g. `OfficeLocation`)

1. Edit `src/backend/Auth/Models/AuthSessionDto.cs` — add `public string? OfficeLocation { get; set; }`.
2. Edit `src/backend/Libraries/Domain/Dto/AuthDto.cs` — add the same property. The Main API deserializes through this DTO.
3. Edit `src/backend/Auth/Services/AuthSessionService.cs:34-41` — populate the new field inside the `sessionDto` initializer in `IssueSessionAsync`.
4. Edit `src/backend/Auth/Controllers/AuthController.cs:155-162` — populate the same field in `CreateTestSession` so dev-only sessions don't silently miss it.
5. Edit `src/backend/Libraries/Shared/Globals/Constants.cs` — add `public const string KeySessionOfficeLocation = "session.officeLocation";`.
6. Edit `src/backend/API/Middleware/SessionValidationMiddleware.cs:118-128` — add `context.Items[Constants.KeySessionOfficeLocation] = authDto.OfficeLocation;` inside the `ValidateSessionAsync` happy path.
7. Edit `src/backend/Libraries/Services/Services/UserContextService.cs` — add a typed `OfficeLocation` accessor that wraps `HttpContextAccessor.HttpContext?.Items[Constants.KeySessionOfficeLocation]`.
8. Existing sessions in Valkey will return `null` for the new field until they refresh (login again). No migration needed.

## 3. Enable Portal SSO from scratch in a fresh deployment

1. Generate the key pair on the Auth-issuing partner side. Receive the partner's signing public key PEM and provide them your decryption public key PEM (script: `src/backend/Auth/scripts/portal-sso-key-generator.cs` if present, else use OpenSSL).
2. Edit `src/backend/Auth/appsettings.json` — set:
   - `PortalSso:Enabled = true`
   - `PortalSso:LaunchUrlTemplate` to the partner launch URL with `{state}`, `{nonce}`, `{returnUrl}`, `{callbackUrl}` placeholders
   - `PortalSso:Issuer`, `PortalSso:Audience`, `PortalSso:SourceSystemId`
   - `PortalSso:Crypto:DecryptionPrivateKeyPath` (recommend file path, NOT inline PEM in config)
   - `PortalSso:Crypto:SigningPublicKeyPath`
   - `PortalSso:Crypto:RequiredOuterAlg = "RSA-OAEP-256"` (or whatever the partner uses)
   - `PortalSso:Crypto:RequiredEnc = "A256GCM"`
   - `PortalSso:Crypto:RequiredInnerAlg = "RS256"`
   - `PortalSso:ExchangeApi:BaseUrl`, `Path`, `SubscriptionHeaderName`, `SubscriptionKey`, `SourceHeaderName`
   - `PortalSso:AllowedIpRanges` (CIDR list of partner egress IPs — falls open if empty, see `IsRemoteIpAllowed`)
   - `PortalSso:AllowedSourceUrls` (whitelist of allowed `sourceUrl` claim values — falls open if empty, see `ValidateSourceUrl`)
   - `PortalSso:DefaultReturnUrl`, `PortalSso:CallbackUrl`
3. Make the key files readable by the Auth process user only (`chmod 0400` on Linux, restricted ACL on Windows).
4. Add the partner's IP range to `AllowedIpRanges` BEFORE go-live — leaving it empty disables the IP guard entirely.
5. Verify by calling `GET /api/Auth/SsoStart` — it should return a `launchUrl` containing the partner's hostname.

## 4. Replace the IDP backend (e.g. swap NIE IDP for Azure AD)

1. The current IDP integration lives entirely in `AuthController.Login` (`POST /api/Auth/Login`) — it calls `{NIEAuthApi:BaseUrl}/LogInUser` with `x-nie-aws-api-gw-key`. Replace this method body with the new IDP's authentication call. Map the response into `LoginResponse` shape (`isAuthenticated`, `userId`, `fullName`, `email`, `department`, optional `sessionToken`).
2. `AuthSessionService.IssueSessionAsync` does NOT need to change — it consumes `LoginResponse` and writes `session:{token}` regardless of source.
3. Update `AuthController.Refresh` (`POST /api/Auth/Refresh`) to call the new IDP's refresh endpoint and adjust `RefreshResponseRoot` accordingly.
4. If the new IDP is OAuth2 / OIDC with ID-token flow, prefer adding it as a second SSO path (`SsoStart` / `SsoCallback`) rather than replacing `Login` — that gives you both pwd and SSO and keeps the audit log split clean.
5. Update the Auth FE login form (`src/frontend/auth/src/components/LoginPage.vue`) if the new IDP needs different fields (e.g. tenant code).
6. Update `appsettings.json` config keys (`NIEAuthApi:*`) and remove the old subscription-key handling from `AuthController` constructor lines 43–46 if no longer relevant.

## 5. Redirect to a different post-login page

1. Edit `src/frontend/auth/src/components/LoginPage.vue` — locate the success handler that issues `window.location.href = ...`. Change the destination URL.
2. For SSO, edit `src/backend/Auth/appsettings.json` — change `PortalSso:DefaultReturnUrl`. The `BuildReturnUrl` helper in `PortalSsoService.cs:399-413` appends `?sso=1&state=...` automatically.
3. For per-request return URL override (e.g. deep-linking back to a specific page after SSO), pass `?returnUrl=...` to `GET /api/Auth/SsoStart`. The validator only rewrites the URL if explicitly provided; otherwise it falls back to the configured default.
