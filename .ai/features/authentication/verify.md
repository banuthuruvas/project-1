# Authentication — Verify

Use this when you change anything in `Auth/`, `Middleware/SessionValidationMiddleware.cs`, or any session-touching code.

## Backend

```bash
dotnet build src/backend/NieTemplate.sln
dotnet run --project src/backend/Auth
dotnet run --project src/backend/API
```

Check the logs:

- Auth API should log `Now listening on: http://localhost:5001`.
- Main API should log `Now listening on: http://localhost:5002`.
- If `PortalSso:Enabled = true`, the Auth log on first SSO request should NOT print `PortalSso is not enabled`.

## API smoke — username / password path

```bash
# 1. Mint a dev session (Development environment only)
SESSION=$(curl -s -X POST http://localhost:5001/api/Auth/CreateTestSession \
  -H "Content-Type: application/json" \
  -d '{"UserId":"devia","Name":"Devi Anggraini","Email":"devi@nie.edu.sg","Department":"Digital Solutions"}' \
  | jq -r .sessionToken)

echo "Session: $SESSION"

# 2. Confirm the session is in Valkey by hitting Verify
curl -s http://localhost:5001/api/Auth/Verify -H "X-Session-Id: $SESSION" | jq

# 3. Confirm the Main API accepts the same session
curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5002/api/AccessControl/GetCurrentAccessProfile \
  -H "X-Session-Id: $SESSION"
# Expect: 200

# 4. Confirm a missing session is rejected
curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5002/api/AccessControl/GetCurrentAccessProfile
# Expect: 401

# 5. Confirm a bogus session is rejected
curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5002/api/AccessControl/GetCurrentAccessProfile \
  -H "X-Session-Id: not-a-real-token"
# Expect: 401
```

## API smoke — Portal SSO path (only when configured)

```bash
# 1. Start an SSO request
curl -s "http://localhost:5001/api/Auth/SsoStart?returnUrl=https://localhost:8001/" | jq
# Expect: { state, nonce, launchUrl, pollIntervalMs }

# 2. The browser should be redirected to launchUrl by the FE.
# 3. The partner POSTs to /api/Auth/SsoCallback (do not invoke manually with real keys).
# 4. The FE polls /api/Auth/SsoFinalize?state=<state> until status === "Completed" or "Failed".
```

`POST /api/Auth/SsoCallback` returns `503` when SSO is disabled, `502` if the partner exchange API fails, and `401` for any of: unknown state, invalid signature, replay, IP not allowlisted, source-system mismatch.

## Session refresh

```bash
curl -s -X POST http://localhost:5001/api/Auth/Refresh \
  -H "Content-Type: application/json" \
  -d "\"$SESSION\""
# Expect: 200 with the new sessionToken in the body, OR 401 if the IDP refused.
```

## Frontend (manual click-path)

1. Start the full stack via the `🚀 All Services (Hot Reload)` task.
2. Open `http://localhost:8002` (Auth FE) — the login form should render.
3. Submit valid credentials — the page should set `SessionId` and `User` cookies for the configured `VITE_COOKIE_DOMAIN` and redirect to `http://localhost:8001`.
4. Open browser DevTools → Application → Cookies — confirm `SessionId` and `User` exist with `Path=/` and the right domain.
5. Open the Network tab and click any page — every request to `:5002` should carry `X-Session-Id: <token>`.
6. Click profile menu → Logout — cookies should be cleared and the browser should redirect back to the Auth FE.
7. Manually expire the session by deleting the `SessionId` cookie and clicking a sidebar link — the FE should redirect to the Auth FE login page.

## Audit and observability

- `AuditLog` table: confirm a `Login` row (`Action = 10`, `Category = Authentication`) appears for the user that just logged in (the audit logger may live in the Main API; check `AuditLogger.LogLoginAsync`).
- Sentry: if `Sentry:Dsn` is configured, a forced exception in `AuthController.Login` should appear in the dashboard within 60 seconds.
- Valkey CLI inspection (NEVER paste real tokens): `redis-cli -p 6379 KEYS "session:*"` should show one row per active user.

## Permissions

- A user with no roles still gets a valid session — the Main API will return `403` from any `[RequireAccessFunction(...)]` endpoint until a role is assigned. `GetCurrentAccessProfile` is intentionally NOT gated by `RequireAccessFunction` so the FE can show the empty state.
