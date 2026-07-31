#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

dotnet build src/backend/NieTemplate.sln

if [ $(grep -rn "SsrfGuard\.Validate" src/backend/Libraries/Services/Services/MyInfo/ src/backend/Auth/Services/ 2>/dev/null | wc -l) -lt 2 ]; then
  echo "FAIL: SsrfGuard.Validate not wired into MyInfo and PortalSso"
  exit 1
fi

echo "verify.sh: OK (SSRF guard present)"
