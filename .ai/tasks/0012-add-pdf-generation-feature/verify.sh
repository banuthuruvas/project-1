#!/usr/bin/env bash
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

test -f src/backend/Libraries/Services/Services/PdfGeneration/IPdfGenerationService.cs \
  || { echo "FAIL: IPdfGenerationService.cs missing"; exit 1; }
test -f src/backend/Libraries/Services/Services/PdfGeneration/PlaywrightPdfGenerationService.cs \
  || { echo "FAIL: PlaywrightPdfGenerationService.cs missing"; exit 1; }
test -f src/backend/API/Controllers/ReportController.cs \
  || { echo "FAIL: ReportController.cs missing"; exit 1; }
test -f src/frontend/main/src/services/reportService.ts \
  || { echo "FAIL: reportService.ts missing"; exit 1; }

grep -q "IPdfGenerationService" src/backend/API/Program.cs \
  || { echo "FAIL: IPdfGenerationService not registered"; exit 1; }

dotnet build src/backend/NieTemplate.sln
pnpm --filter main type-check

echo "verify.sh: OK (pdf-generation feature wired)"
