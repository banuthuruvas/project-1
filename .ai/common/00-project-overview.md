# 00 — Project Overview

NIE Template is a production-ready, full-stack monorepo template for building enterprise web applications across NIE. Every new project in the organization clones this template, adopts its versioning contract, then trims and customizes via the task system in `.ai/tasks/`.

## Stack at a glance

| Layer | Technology |
| --- | --- |
| Backend API | .NET 10, ASP.NET Core, Entity Framework Core, Mapster |
| Auth API | .NET 10 + Valkey-backed sessions, optional Singpass Portal SSO |
| Frontend | Vue 3 + Composition API + TypeScript + Vite + Tailwind CSS, pnpm monorepo |
| Database | PostgreSQL 16+ |
| Cache / sessions | Valkey (Redis-compatible) |
| Background jobs | TickerQ |
| Observability | OpenTelemetry + Sentry |
| Testing | Playwright (API + E2E) |
| CI/CD | Jenkins + Docker + nginx |

## Local service ports

| Service | Port | URL |
| --- | --- | --- |
| Main API | 5002 | http://localhost:5002/swagger |
| Auth API | 5001 | http://localhost:5001/swagger |
| Main Frontend | 8001 | http://localhost:8001 |
| Auth Frontend | 8002 | http://localhost:8002 |
| PostgreSQL | 5432 | — |
| Valkey | 6379 | — |

Long-running services start via `.vscode/launch.json` → `🚀 All Services (Hot Reload)`. Do not invent ad-hoc commands.

## Reference samples

The template ships with a **Procurement** reference sample (vendors, catalog items, purchase orders) so cloned projects have working CRUD, audit, file-upload, and approval-workflow examples to learn from. Once a derived project has built its own real entities, it removes procurement via [`tasks/0003-remove-procurement-samples`](../tasks/0003-remove-procurement-samples/). Procurement stays in the template itself.
