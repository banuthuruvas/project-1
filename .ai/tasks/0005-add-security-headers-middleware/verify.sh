#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

dotnet build src/backend/NieTemplate.sln

# Live smoke (assumes services are running locally on 5002)
COUNT=$(curl -sI http://localhost:5002/health 2>/dev/null \
  | grep -iE 'strict-transport|content-security|x-content-type|x-frame|referrer-policy|permissions-policy' \
  | wc -l || true)

if [ "$COUNT" -lt 6 ]; then
  echo "FAIL: expected ≥ 6 security headers, got $COUNT"
  exit 1
fi
echo "verify.sh: OK (security headers present)"
