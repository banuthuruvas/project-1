# PDF Generation & Reports

> **Status:** `optional` | **Source:** i3g PDF services + Playwright HTML-to-PDF

## Overview

Generates downloadable PDF reports from HTML using Playwright headless Chromium. Includes a Reports sidebar menu with grouped report cards, filter-based generation, and A4 print preview.

## Key Files

| Layer      | Path                                                       | Purpose                                                            |
| ---------- | ---------------------------------------------------------- | ------------------------------------------------------------------ |
| Service    | `Services/PdfGeneration/IPdfGenerationService.cs`          | Interface: GeneratePdfFromHtml, GetReportTypes, GenerateReportHtml |
| Service    | `Services/PdfGeneration/PlaywrightPdfGenerationService.cs` | HTML→PDF via Playwright CLI                                        |
| Controller | `API/Controllers/ReportController.cs`                      | GET types, POST preview, POST download                             |
| FE Page    | `pages/reports/ReportsIndex.vue`                           | Report type cards grouped by category                              |
| FE Page    | `pages/reports/ReportDetail.vue`                           | Filters + A4 preview + download/print                              |
| FE Service | `services/reportService.ts`                                | API client                                                         |
| Config     | `build/Dockerfile.api`                                     | Playwright Chromium dependencies                                   |

## Report Types

- **Procurement:** PO Summary, Vendor Analysis, Spending by Dept, Approval Timeline
- **Audit:** Audit Trail Report, User Activity Log
- **System:** Health Status, Error Summary
