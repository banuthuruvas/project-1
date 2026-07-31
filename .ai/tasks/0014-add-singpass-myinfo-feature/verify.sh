#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

# Dependency check: SSRF guard (task 0006) must be present
test -f src/backend/Libraries/Shared/Helpers/SsrfGuard.cs \
  || { echo "FAIL: task 0006 (SSRF allowlist) must be applied first"; exit 1; }

# Structural checks
test -f src/backend/Libraries/Services/Services/MyInfo/IMyInfoService.cs \
  || { echo "FAIL: IMyInfoService.cs missing"; exit 1; }
test -f src/backend/Libraries/Services/Services/MyInfo/MyInfoService.cs \
  || { echo "FAIL: MyInfoService.cs missing"; exit 1; }
test -f src/backend/API/Controllers/MyInfoController.cs \
  || { echo "FAIL: MyInfoController.cs missing"; exit 1; }
test -f src/frontend/main/src/services/myInfoService.ts \
  || { echo "FAIL: frontend myInfoService.ts missing"; exit 1; }

grep -q "IMyInfoService" src/backend/API/Program.cs \
  || { echo "FAIL: IMyInfoService not registered"; exit 1; }
grep -q "SsrfGuard" src/backend/Libraries/Services/Services/MyInfo/MyInfoService.cs \
  || { echo "FAIL: MyInfoService must invoke SsrfGuard on outbound calls"; exit 1; }

# Anti-checks: no real JWKS keys committed
if [ -f src/backend/API/MyInfo/Jwks/private-jwks.json ]; then
  if grep -q '"d"' src/backend/API/MyInfo/Jwks/private-jwks.json; then
    echo "FAIL: real EC private key (d-component) detected in committed JWKS — secrets must be external"
    exit 1
  fi
fi

dotnet build src/backend/NieTemplate.sln
pnpm --filter main type-check

echo "verify.sh: OK (singpass-myinfo feature wired with SSRF guard)"
