# Singpass MyInfo — Customize

## 1. Onboard MyInfo for the first time (staging)

1. Register the application in the Singpass partner portal (separate from this codebase). You will receive:
   - A `client_id`
   - A registered `redirect_uri`
   - The expected scopes / attributes
2. Generate two EC P-256 key pairs (one for `use=sig`, one for `use=enc`). Example with `openssl`:
   ```bash
   openssl ecparam -name prime256v1 -genkey -noout -out signing-private.pem
   openssl ec -in signing-private.pem -pubout -out signing-public.pem
   openssl ecparam -name prime256v1 -genkey -noout -out enc-private.pem
   openssl ec -in enc-private.pem -pubout -out enc-public.pem
   ```
3. Convert each pair to JWK form (use a tool like `jose-util` or `pem-jwk`). Combine them into a single JSON file with shape:
   ```json
   {
     "keys": [
       { "kid": "sig-2026-01", "use": "sig", "kty": "EC", "crv": "P-256", "x": "...", "y": "...", "d": "...", "alg": "ES256" },
       { "kid": "enc-2026-01", "use": "enc", "kty": "EC", "crv": "P-256", "x": "...", "y": "...", "d": "...", "alg": "ECDH-ES+A256KW" }
     ]
   }
   ```
4. Place the file outside the source tree (e.g. `/etc/myapp/myinfo/private-jwks.json`) with restricted ACL.
5. Publish the matching public JWKS (the `d` field stripped) at a URL Singpass can fetch — many teams expose `/myinfo/.well-known/jwks.json`. The shipped template does NOT host this endpoint; add a `[HttpGet("/.well-known/myinfo-jwks.json")]` controller if needed.
6. Edit `src/backend/API/appsettings.json`:
   ```json
   "MyInfo": {
     "ClientId": "STG-201712345E-MYAPP",
     "RedirectUri": "https://staff.myapp.example/myinfo/callback",
     "BaseUrl": "https://stg-id.singpass.gov.sg",
     "Scopes": "openid name email mobileno",
     "JwtClientAuthentication": {
       "PrivateJwksPath": "/etc/myapp/myinfo/private-jwks.json",
       "SigningKeyId": "sig-2026-01",
       "EncryptionKeyId": "enc-2026-01"
     }
   }
   ```
7. Restart the API. Hit `GET /api/MyInfo/IsConfigured` — expect `{ "configured": true }`.
8. Open `MyInfoPage.vue` and click the start button. Confirm the browser is redirected to Singpass staging.

## 2. Narrow the requested attributes

The default scope list is broad. To request only name + mobile:

1. Edit `appsettings.json` `MyInfo:Scopes`:
   ```json
   "Scopes": "openid name mobileno"
   ```
2. Restart. The `BuildScopeString` helper in `MyInfoService.cs:1167-1193` deduplicates and always inserts `openid` at the front; you don't need to repeat it.
3. The returned `MyInfoPersonData` will only have `Name` and `MobileNumber` populated; other fields will be null. The FE rendering in `MyInfoPage.vue` already conditionals over null fields.

## 3. Rotate keys

1. Generate the new EC pair as in step 2 of customization 1.
2. Add the NEW key to the JWKS file with a new `kid`, leaving the OLD key in place (so in-flight requests can still validate).
3. Publish the updated public JWKS so Singpass can fetch the new one.
4. Once Singpass has confirmed the rotation, edit `MyInfo:JwtClientAuthentication:SigningKeyId` (or `EncryptionKeyId`) to the new `kid`. Restart the API.
5. After 30 minutes (longer than `ClientKeyCacheLifetime = 15m`), remove the old key from the JWKS file.

## 4. Switch from staging to production

1. In `appsettings.Production.json` (or the equivalent secrets file):
   ```json
   "MyInfo": {
     "BaseUrl": "https://id.singpass.gov.sg",
     "ClientId": "PROD-201712345E-MYAPP",
     "RedirectUri": "https://staff.myapp.production/myinfo/callback"
   }
   ```
2. The `NormalizeAuthorityBaseUrl` helper in `MyInfoService.cs:1127-1153` maps the legacy `api.myinfo.gov.sg` host to `https://id.singpass.gov.sg` automatically — but prefer the canonical `id.singpass.gov.sg` URL in config.
3. Provision a separate production private JWKS file. Never reuse staging keys.

## 5. Add a new field to `MyInfoPersonData`

If Singpass adds a new attribute (e.g. `housingtype`):

1. Edit `MyInfoPersonData.cs` to add `public string? HousingType { get; set; }`.
2. Edit `MyInfoService.ParsePersonData` (line 916-948) — add `HousingType = GetMyInfoValue(root, "housingtype"),` to the initializer.
3. Add `housingtype` to `MyInfo:Scopes` in `appsettings.json`.
4. Update the FE `myInfoService.ts` interface and the rendering in `MyInfoPage.vue`.

## 6. Disable MyInfo entirely (without removing the feature)

Set `MyInfo:ClientId = ""` (or just remove the section). `MyInfoService.IsConfigured` returns false; the controller returns `400 { message: "MyInfo/Singpass is not configured" }` for both endpoints. Hide the `/myinfo` route in the FE by gating it on `myInfoService.isConfigured()`.

## 7. Manual JWE fallback failure investigation

If a JWE fails both library decryption and the manual ECDH-ES path:

1. Capture the `alg` / `enc` from the log line `MyInfo JWE header: alg={Alg}, enc={Enc}, kid={Kid}, parts={Parts}`.
2. Confirm the algorithm pair is supported in `ManualDecryptAndValidateAsync`. Currently:
   - Key wrap: `ECDH-ES+A128KW`, `ECDH-ES+A192KW`, `ECDH-ES+A256KW`
   - Content: `A128GCM`, `A192GCM`, `A256GCM`, `A128CBC-HS256`, `A192CBC-HS384`, `A256CBC-HS512`
3. If Singpass switches to a new enc you'll need to add a branch in `MyInfoService.cs:431-468` (the switch on `alg` for key sizes and the switch on `enc` for content decryption).
