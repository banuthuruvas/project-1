# Task 0012 — Add PDF Generation Feature

> **Status:** scaffolded — opt-in. Playwright in production requires the headless Chromium runtime in the container image; do not adopt without provisioning that.

> **Why:** i3g and other apps generate downloadable reports via HTML→PDF. This task ships the canonical service contract + Playwright implementation + report controller so derived repos can stop forking i3g's logic.

## Pre-checks

```bash
test ! -f src/backend/Libraries/Services/Services/PdfGeneration/IPdfGenerationService.cs \
  || { echo "Already added; skipping."; exit 0; }
```

## 1. Files to create

```text
src/backend/Libraries/Services/Services/PdfGeneration/IPdfGenerationService.cs
src/backend/Libraries/Services/Services/PdfGeneration/PlaywrightPdfGenerationService.cs
src/backend/API/Controllers/ReportController.cs
src/backend/Libraries/Domain/Dto/Report/ReportRequestDto.cs
src/frontend/main/src/services/reportService.ts
```

## 2. Files to edit

### `src/backend/API/Program.cs`

```diff
+ builder.Services.AddSingleton<IPdfGenerationService, PlaywrightPdfGenerationService>();
```

**Why:** Playwright browser context is expensive to create; reuse a single instance.

### `build/Dockerfile.api`

Add Playwright Chromium dependencies. Append to the runtime stage:

```diff
+ # Playwright Chromium runtime deps
+ RUN apt-get update && apt-get install -y --no-install-recommends \
+     libnss3 libatk1.0-0 libatk-bridge2.0-0 libcups2 libdrm2 libxkbcommon0 \
+     libxcomposite1 libxdamage1 libxfixes3 libxrandr2 libgbm1 libpango-1.0-0 \
+     libcairo2 libasound2 \
+   && rm -rf /var/lib/apt/lists/*
+
+ # Install Playwright browser
+ RUN dotnet tool install --global Microsoft.Playwright.CLI \
+   && /root/.dotnet/tools/playwright install chromium --with-deps
```

**Why:** `PlaywrightPdfGenerationService` shells out to Chromium; missing libs fail at runtime, not build time.

## 3. Verification

```bash
dotnet build src/backend/NieTemplate.sln
pnpm --filter main type-check
grep -n "IPdfGenerationService" src/backend/API/Program.cs   # ≥1
```

Live smoke (services + Chromium installed):

```bash
curl -s -X POST http://localhost:5002/api/Report/generate \
  -H 'Content-Type: application/json' \
  -d '{"reportType":"po-summary","filters":{}}' \
  -o /tmp/test.pdf
file /tmp/test.pdf | grep -q "PDF document"   # PDF magic bytes
```

## 4. Rollback

```bash
git restore --staged --worktree \
  src/backend/Libraries/Services/Services/PdfGeneration/ \
  src/backend/API/Controllers/ReportController.cs \
  src/backend/Libraries/Domain/Dto/Report/ \
  src/frontend/main/src/services/reportService.ts \
  src/backend/API/Program.cs \
  build/Dockerfile.api
```

## Maintainer review checklist before promoting to a release

- [ ] CI image build verified to install Chromium successfully
- [ ] Cold-start render time measured; tune browser-pool size if > 2s p50
- [ ] Report endpoint guarded with `[RequireAccessFunction("report:generate")]`
- [ ] Generated PDFs stripped of metadata (Producer/Author) for privacy
- [ ] Concurrency limit on the controller (rate-limiting partition + max parallelism)
