#!/usr/bin/env bash
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

echo "→ dotnet build"
dotnet build src/backend/NieTemplate.sln

echo "→ no SampleModel references"
if grep -rn "SampleModel\|SampleChildModel\|ESampleEnum\|ISampleModelService" src/ tests/ 2>/dev/null; then
  echo "FAIL: residual SampleModel references found"
  exit 1
fi

echo "→ no AISolution namespace"
if grep -rn "Domain\.Services\.AISolution" src/ 2>/dev/null; then
  echo "FAIL: residual Domain.Services.AISolution references found"
  exit 1
fi

echo "→ frontend type-check"
( cd src/frontend && pnpm --filter main type-check )

echo "verify.sh: OK"
