#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

dotnet build src/backend/NieTemplate.sln

if grep -rn '"Manager"\|"Finance"\|"Procurement"' \
     src/backend/API/Controllers/PurchaseOrderController.cs \
     src/backend/Libraries/Services/Services/PurchaseOrder/ \
     src/frontend/main/src/staff/pages/staff/ApprovalQueue.vue 2>/dev/null; then
  echo "FAIL: residual approval-stage string literals found"
  exit 1
fi

( cd src/frontend && pnpm --filter main type-check )

echo "verify.sh: OK (EApprovalStage enum applied)"
