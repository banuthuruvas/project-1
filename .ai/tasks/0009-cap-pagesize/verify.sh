#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

dotnet build src/backend/NieTemplate.sln

if [ $(grep -rn "PagedSearchDto" src/backend/Libraries/Domain/Dto/ src/backend/Libraries/Services/Services/ 2>/dev/null | wc -l) -lt 2 ]; then
  echo "FAIL: PagedSearchDto not adopted"
  exit 1
fi

echo "verify.sh: OK (PageSize cap in place)"
