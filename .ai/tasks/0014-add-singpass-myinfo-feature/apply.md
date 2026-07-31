# Task 0014 — Add Singpass MyInfo Feature

> **Status:** scaffolded — strictly opt-in. **Do not apply** unless the derived repo serves Singapore citizens and has client credentials registered with Singpass.

> **Why:** MyInfo is FAPI-grade — PAR, DPoP, JWE, JWS client assertion. Hand-rolling it per project produces drift and security risk. This task ships the canonical implementation and forces the SSRF guard wiring (task 0006) to be in place before adopting.

The full feature dossier is [`.ai/features/singpass-myinfo/`](../../features/singpass-myinfo/). Read [`do-dont.md`](../../features/singpass-myinfo/do-dont.md) before touching crypto.

## Pre-checks

```bash
test -f src/backend/Libraries/Shared/Helpers/SsrfGuard.cs \
  || { echo "FAIL: task 0006 (SSRF allowlist) must be applied first"; exit 1; }
test ! -f src/backend/Libraries/Services/Services/MyInfo/MyInfoService.cs \
  || { echo "Already added; skipping."; exit 0; }
```

## 1. Files to create

```text
src/backend/Libraries/Services/Services/MyInfo/IMyInfoService.cs
src/backend/Libraries/Services/Services/MyInfo/MyInfoService.cs
src/backend/API/Controllers/MyInfoController.cs
src/backend/API/MyInfo/Jwks/private-jwks.json.example   ← example only, never the real key
src/frontend/main/src/staff/pages/staff/MyInfoPage.vue
src/frontend/main/src/staff/pages/staff/MyInfoCallback.vue
src/frontend/main/src/services/myInfoService.ts
```

The actual `private-jwks.json` (with real EC keys) is **never committed** — provision it via secret store / sealed secret / Kubernetes secret mount and reference its path in configuration.

## 2. Files to edit

### `src/backend/API/Program.cs`

```diff
+ builder.Services.Configure<MyInfoOptions>(configuration.GetSection("MyInfo"));
+ builder.Services.AddHttpClient<IMyInfoService, MyInfoService>();
+ builder.Services.AddMemoryCache(); // if not already added — required for discovery + JWKS caching
```

**Why:** typed HttpClient ensures the SSRF guard (from task 0006) is invoked on every outbound call.

### `build/appsettings.api.json`

```diff
+ "MyInfo": {
+   "ClientId": "",
+   "RedirectUri": "https://your-app.example.com/myinfo/callback",
+   "DiscoveryUrl": "https://test.api.myinfo.gov.sg/.well-known/openid-configuration",
+   "Scopes": "uinfin name sex race nationality dob email mobileno regadd",
+   "JwtClientAuthentication": {
+     "PrivateJwksPath": "/etc/secrets/myinfo/private-jwks.json",
+     "SigningKeyId": "sig-2026",
+     "EncryptionKeyId": "enc-2026"
+   },
+   "OutboundAllowlist": [
+     "https://test.api.myinfo.gov.sg",
+     "https://api.myinfo.gov.sg"
+   ]
+ }
```

**Why:** `OutboundAllowlist` is consumed by `SsrfGuard.Validate` from task 0006. **Production must use the prod URL only.**

## 3. Verification

```bash
dotnet build src/backend/NieTemplate.sln
pnpm --filter main type-check
grep -n "IMyInfoService" src/backend/API/Program.cs
grep -n "SsrfGuard" src/backend/Libraries/Services/Services/MyInfo/MyInfoService.cs   # must be ≥1
```

Boot smoke without Singpass credentials (the `IsConfigured` endpoint should return `false`):

```bash
curl -s http://localhost:5002/api/MyInfo/IsConfigured
# {"configured":false,"reason":"PrivateJwksPath unreadable"}
```

## 4. Rollback

```bash
git restore --staged --worktree \
  src/backend/Libraries/Services/Services/MyInfo/ \
  src/backend/API/Controllers/MyInfoController.cs \
  src/backend/API/MyInfo/ \
  src/frontend/main/src/staff/pages/staff/MyInfo*.vue \
  src/frontend/main/src/services/myInfoService.ts \
  src/backend/API/Program.cs \
  build/appsettings.api.json
```

## Maintainer review checklist before promoting to a release

- [ ] Real production JWKS is provisioned via secret store, never committed
- [ ] `OutboundAllowlist` matches the actual MyInfo environment for the target deployment
- [ ] DPoP nonce caching uses a backing store with TTL ≥ 10 minutes (Valkey, not in-process)
- [ ] Audit log records every Callback success/failure including `state` (no PII in the log line)
- [ ] CI runs `MyInfoService` unit tests with example JWKS to verify decode paths
- [ ] Security review sign-off captured on the task release notes
