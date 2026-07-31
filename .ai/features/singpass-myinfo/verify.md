# Singpass MyInfo — Verify

This file does NOT include real Singpass credentials, real `client_id`s, or real keys. All examples use placeholders.

## Backend boot (no credentials)

```bash
dotnet build src/backend/NieTemplate.sln
dotnet run --project src/backend/API
```

Without configuration, the service should boot cleanly and `IsConfigured` should return false:

```bash
SESSION=$(curl -s -X POST http://localhost:5001/api/Auth/CreateTestSession \
  -H "Content-Type: application/json" \
  -d '{"UserId":"devia"}' | jq -r .sessionToken)

curl -s http://localhost:5002/api/MyInfo/IsConfigured \
  -H "X-Session-Id: $SESSION" | jq
# Expect: { "configured": false }

curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5002/api/MyInfo/GetAuthorizeUrl \
  -H "X-Session-Id: $SESSION"
# Expect: 400 (with body "MyInfo/Singpass is not configured")
```

## Backend boot (configured against staging)

After completing the steps in `customize.md` § 1:

```bash
curl -s http://localhost:5002/api/MyInfo/IsConfigured \
  -H "X-Session-Id: $SESSION" | jq
# Expect: { "configured": true }

curl -s "http://localhost:5002/api/MyInfo/GetAuthorizeUrl" \
  -H "X-Session-Id: $SESSION" | jq
# Expect: { "authorizeUrl": "https://stg-id.singpass.gov.sg/...?..." }
```

The `authorizeUrl` should point at the partner host configured in `MyInfo:BaseUrl` and contain `client_id` and `request_uri` (FAPI/PAR path) or `code_challenge` + `code_challenge_method=S256` + `state` + `nonce` (non-FAPI fallback).

## Valkey state inspection

```bash
# After GetAuthorizeUrl returns, the state should be cached
redis-cli -p 6379 KEYS "myinfo:state:*"
# Expect at least one key with TTL <= 600s

redis-cli -p 6379 TTL "myinfo:state:<theStateId>"
# Expect a value <= 600 (10 minutes)
```

## Manual end-to-end (staging only)

You need to be a registered Singpass test user. Do NOT do this with production credentials in a dev environment.

1. From `MyInfoPage.vue` click "Verify with Singpass".
2. The browser opens the Singpass staging login.
3. Log in with a test account.
4. Singpass redirects to `MyInfoCallback.vue?code=...&state=...`.
5. The callback page posts to `/api/MyInfo/Callback`.
6. The page renders `MyInfoPerson` data (name, NRIC/FIN, etc.).
7. Confirm the cached state record is gone:
   ```bash
   redis-cli -p 6379 GET "myinfo:state:<theStateId>"
   # Expect (nil)
   ```

## Negative tests

```bash
# Replay the same state — should fail
curl -s -X POST http://localhost:5002/api/MyInfo/Callback \
  -H "Content-Type: application/json" \
  -H "X-Session-Id: $SESSION" \
  -d '{"AuthCode":"any","State":"<the-already-used-state>"}'
# Expect: 400 { message: "MyInfo state is invalid or has already been used" }

# Expired state (after 10 minutes)
curl -s -X POST http://localhost:5002/api/MyInfo/Callback \
  -H "Content-Type: application/json" \
  -H "X-Session-Id: $SESSION" \
  -d '{"AuthCode":"any","State":"<expired-state>"}'
# Expect: 400 { message: "MyInfo state is invalid or has already been used" }
```

## Frontend (no Singpass)

1. With MyInfo unconfigured, open `/myinfo`. The page should display a friendly "MyInfo/Singpass is not configured" message and disable the start button. (If the FE doesn't yet render this state gracefully, the page will throw on the 400 response — fix the page to call `myInfoService.isConfigured()` first.)
2. Confirm the route is gated. If you want only Singapore-citizens-handling staff to see it, add a `screen.myinfo.view` access function and wire it into the route's `meta`.

## Logs to watch

- `MyInfo JWE header: alg=..., enc=..., kid=..., parts=5` — appears once per Callback call. Confirms the JWE header parsed.
- `MyInfo pushed authorization request failed with status...` — appears when PAR fails. Inspect the body for `invalid_client` (key mismatch) or `invalid_request` (scope/redirect mismatch).
- `MyInfo userinfo request failed with status...` — appears when the userinfo call fails. The error message "Your MyInfo profile has not been set up yet" is a known Singpass staging quirk for new test users.

## No production keys in the repo

```bash
# Confirm the shipped JWKS is a placeholder
diff src/backend/API/MyInfo/Jwks/private-jwks.json <(echo "")
# Should differ — but visually inspect the kid values to confirm they are sample keys.

# Confirm the production JWKS is NEVER committed
grep -r "PROD-" src/backend/API/MyInfo/Jwks/
# Expect: no matches in the template
```
