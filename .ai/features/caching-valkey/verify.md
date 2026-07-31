# Caching (Valkey) — Verify

## Reachability

```bash
# Local Valkey via Docker
docker run -d --name nietemplate-valkey -p 6379:6379 valkey/valkey:latest

redis-cli -p 6379 PING
# Expect: PONG
```

## Backend

```bash
dotnet build src/backend/NieTemplate.sln
dotnet run --project src/backend/Auth   # port 5001
dotnet run --project src/backend/API    # port 5002
```

## Health endpoint

```bash
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/health
# Expect: 200 (Postgres + Valkey both reachable)

# Stop Valkey to confirm the probe fails
docker stop nietemplate-valkey
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/health
# Expect: 503
docker start nietemplate-valkey
```

## Session write/read across processes

```bash
# Auth API writes a session
SESSION=$(curl -s -X POST http://localhost:5001/api/Auth/CreateTestSession \
  -H "Content-Type: application/json" \
  -d '{"UserId":"devia"}' | jq -r .sessionToken)

# Confirm the key is in Valkey (with InstanceName prefix)
redis-cli -p 6379 KEYS "nietemplate:session:*"
# Expect: at least one key. If using "nietemplate:" InstanceName, the prefix is included.

# Inspect the value
redis-cli -p 6379 GET "nietemplate:session:$SESSION"
# Expect: a JSON blob with userId, name, email, lastActive

# Confirm TTL
redis-cli -p 6379 TTL "nietemplate:session:$SESSION"
# Expect: a positive integer (ValidSessionTimeInMins * 60 seconds)

# Main API reads the session — confirm via a real call
curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5002/api/AccessControl/GetCurrentAccessProfile \
  -H "X-Session-Id: $SESSION"
# Expect: 200
```

## Access function cache invalidation

```bash
# Trigger first call (populates cache)
curl -s http://localhost:5002/api/AccessControl/GetCurrentAccessProfile \
  -H "X-Session-Id: $SESSION" | jq .accessFunctionCodes

redis-cli -p 6379 GET "nietemplate:user_access_functions_devia"
# Expect: JSON array of codes

# Update the user's role (assign Admin)
curl -s -X POST http://localhost:5002/api/AccessControl/AssignRole \
  -H "Content-Type: application/json" \
  -H "X-Session-Id: $SESSION" \
  -d '{"userId":"devia","roleId":1}'

# Confirm the cache was invalidated
redis-cli -p 6379 GET "nietemplate:user_access_functions_devia"
# Expect: (nil) or a fresh entry on next call
```

## SSO state lifecycle (only when SSO configured)

```bash
# Trigger SsoStart
curl -s "http://localhost:5001/api/Auth/SsoStart" | jq

# Inspect the state record
redis-cli -p 6379 KEYS "nietemplate:sso:state:*"
redis-cli -p 6379 GET "nietemplate:sso:state:<theStateValue>"
# Expect: a JSON SsoStateRecord

# After successful callback, the state moves to Completed (can be re-fetched)
# After expiry (StateTtlMinutes default 10), the key is gone:
redis-cli -p 6379 TTL "nietemplate:sso:state:<theStateValue>"
```

## Replay defense

```bash
# After a successful SsoCallback the inner JWT's jti is recorded
redis-cli -p 6379 KEYS "nietemplate:sso:jti:*"
# A re-played callback with the same jti is rejected — see authentication/verify.md
```

## InstanceName prefix verification

```bash
# Both Auth and Main MUST set the same Valkey:InstanceName
grep -r "Valkey" src/backend/Auth/appsettings*.json src/backend/API/appsettings*.json
# Expect: matching InstanceName values in both
```

## Connection-failure resilience

```bash
# Stop Valkey while the API is running
docker stop nietemplate-valkey

# The API should NOT crash. AddStackExchangeRedisCache uses AbortOnConnectFail=false.
# Watch the logs — you'll see RedisConnectionException entries.
# Calls to IDistributedCache.GetStringAsync return null; calling code should handle gracefully.

# Specifically, /api/AccessControl/GetCurrentAccessProfile may still work
# (it falls through to DB lookup when cache is empty/unreachable).

curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5002/api/AccessControl/GetCurrentAccessProfile \
  -H "X-Session-Id: $SESSION"
# Expect: still 200 (or 401 if the session expired) — but NOT 500

docker start nietemplate-valkey
```
