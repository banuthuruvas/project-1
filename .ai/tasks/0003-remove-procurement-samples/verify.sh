#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

echo "→ dotnet build"
dotnet build src/backend/NieTemplate.sln

echo "→ no Procurement / Vendor / PurchaseOrder / CatalogItem references"
if grep -rn "Procurement\|PurchaseOrder\|CatalogItem\|Vendor" src/backend/ src/frontend/main/src/ 2>/dev/null; then
  echo "FAIL: residual procurement references found"
  exit 1
fi

echo "→ frontend type-check"
( cd src/frontend && pnpm --filter main type-check )

echo "verify.sh: OK"
