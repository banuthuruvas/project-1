#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

dotnet build src/backend/NieTemplate.sln

if [ $(grep -rn "IOwnedEntity\|EnsureOwnedAsync\|RequireOwnership" src/backend/ 2>/dev/null | wc -l) -lt 4 ]; then
  echo "FAIL: BOLA ownership pattern not wired in"
  exit 1
fi

echo "verify.sh: OK (BOLA pattern present)"
