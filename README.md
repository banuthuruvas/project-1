# NIE Template

A production-ready full-stack application template featuring a .NET 10 backend and a Vue 3 frontend for enterprise web applications.

> **Architectural note (2026-05-03):** This repo is the **data plane** for NIE application scaffolding — it owns the canonical recipes (`.ai/tasks/`, `.ai/features/`, release manifests, Copier scaffold, audit/registry/bot tooling). Its sibling **[nie-ignite](../nie-ignite)** is the **UX plane** (web wizard, AI assist, project history) and consumes recipes from this repo. See [ADR 003](.ai/adrs/003-nie-template-as-data-plane.md).
>
> Use Copier directly from the CLI to scaffold new projects (see below). The Ignite UI is an alternative front door to the same scaffolds.

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22.12+](https://nodejs.org/) and [pnpm 10.33+](https://pnpm.io/)
- [Docker Desktop](https://www.docker.com/) for PostgreSQL and Valkey

### Local Development

```bash
# 1. Start infrastructure
docker-compose -f build/docker-compose.yml up -d postgres valkey

# 2. Setup frontend
cd src/frontend
pnpm install
pnpm build

# 3. Run database migrations
cd ../backend
dotnet ef database update --project Libraries/Data --startup-project API

# 4. Start all services from VS Code
# Run and Debug -> "🚀 All Services (Hot Reload)"
# or use the individual launch configurations in .vscode/launch.json
```

### Access Points

| Service          | URL                           |
| ---------------- | ----------------------------- |
| Main App         | http://localhost:8001         |
| Auth App         | http://localhost:8002         |
| Main API Swagger | http://localhost:5002/swagger |
| Auth API Swagger | http://localhost:5001/swagger |
| pgAdmin          | http://localhost:5050         |

## Architecture

- Frontend: Vue 3 monorepo with `main`, `auth`, `@nietemplate/ui`, and `@nietemplate/shared`
- Backend: .NET 10 solution with `API`, `Auth`, and shared libraries
- Data: PostgreSQL
- Cache and session support: Valkey
- Background processing: Hangfire
- Testing: Playwright API and E2E coverage

## Project Structure

```text
nie-template/
|-- build/                      # Docker, nginx, and deployment assets
|-- docs/                       # Documentation
|-- src/
|   |-- backend/                # .NET 10 backend
|   |-- frontend/               # Vue 3 frontend workspace
|-- tests/                      # Playwright API and E2E tests
`-- .vscode/                    # IDE configuration
```

## Technology Stack

| Layer           | Technology                              |
| --------------- | --------------------------------------- |
| Backend         | .NET 10, Entity Framework Core, Mapster |
| Frontend        | Vue 3, TypeScript, Vite, Tailwind CSS   |
| Database        | PostgreSQL 16+                          |
| Cache           | Valkey                                  |
| Background Jobs | Hangfire                                |
| Testing         | Playwright                              |

## Key Features

- Session authentication with Valkey-backed session management
- Role-based access control
- Audit logging
- File management
- Background jobs with Hangfire
- Code tables and sample CRUD scaffolding
- Shared UI library for reusable Vue components
- Application showcase for CRUD, confirmations, file upload, and reusable screen states

## Documentation

| Document                                                | Description                                                          |
| ------------------------------------------------------- | -------------------------------------------------------------------- |
| [`.ai/README.md`](.ai/README.md)                        | **Start here.** Unified instructions for every AI agent              |
| [`.ai/ALIGN.md`](.ai/ALIGN.md)                          | Paste-into-any-agent self-check prompt for derived repos             |
| [`.ai/common/04-do-and-dont.md`](.ai/common/04-do-and-dont.md) | Hard rules                                                          |
| [`.ai/common/09-template-versioning.md`](.ai/common/09-template-versioning.md) | Release process and downstream adoption          |
| [`.ai/features/`](.ai/features/)                        | One dossier per feature (files map, do/don't, customize, verify)     |
| [`.ai/tasks/`](.ai/tasks/)                              | Executable upgrade tasks for derived repos                           |
| [Change Log](CHANGELOG.md)                              | Human-readable template release history                              |
| [API Reference](docs/API-REFERENCE.md)                  | API endpoint documentation                                           |
| [Contributing](docs/CONTRIBUTING.md)                    | Code style and best practices                                        |
| [Architecture](docs/architecture.md)                    | Project-specific system architecture                                 |
| [Data Model](docs/data-model.md)                        | Entities, relationships, and lifecycle                               |
| [Design Spec](docs/design-spec.md)                      | Service, DTO, and UI integration design                              |
| [Security Model](docs/security-model.md)                | Access functions, threats, and controls                              |
| [Migrations](docs/MIGRATIONS.md)                        | Database migration commands                                          |
| [Portal SSO](docs/PORTAL-SSO-INTEGRATION.md)            | Singpass Portal SSO integration                                      |

## Common Commands

### Backend

```bash
# Run migrations
dotnet ef migrations add <Name> --project src/backend/Libraries/Data --startup-project src/backend/API
dotnet ef database update --project src/backend/Libraries/Data --startup-project src/backend/API

# Build
dotnet build src/backend/NieTemplate.sln
```

### Frontend

```bash
cd src/frontend
pnpm install
pnpm build
pnpm type-check
pnpm dev
```

### Testing

```bash
cd tests
pnpm install
pnpm run install-browsers
pnpm test
pnpm run test:api
pnpm run test:e2e
```

### Template Release Metadata

```bash
python tools/template-versioning/release.py validate
```

### Test Credentials

Set personal or local-only test credentials in `tests/.env.dev.local` when you need authenticated API or E2E coverage.
The committed `tests/.env.dev` file intentionally leaves `TEST_USERNAME` and `TEST_PASSWORD` blank so shared credentials are not stored in the template repository.

## Using as a Template

The recommended path is **Copier** (handles `git clone` + namespace renaming + future updates in one shot):

```bash
pip install --user copier
copier copy --trust gh:NIE/nie-template ./my-app
cd ./my-app && git init && git add . && git commit -m "scaffold"
```

Copier asks for your project name + `dotnet_root_namespace` and runs `tools/template-rename/rename.py` to substitute the placeholders throughout the source. See [`tools/README.md`](tools/README.md) and [`docs/template-distribution.md`](docs/template-distribution.md) for the full model.

Alternatively, the manual path:

1. Clone the repository and remove `.git` if starting a brand-new project.
2. Replace `NieTemplate` with your project name (or run `python tools/template-rename/rename.py --to MyApp`).
3. Keep `.nie-template-version.json` so the derived repo records which NIE template release it is based on.
4. Paste [`.ai/ALIGN.md`](.ai/ALIGN.md) into your AI agent — it will detect missing tasks and apply them with your approval.
5. Review the reusable patterns in [`.ai/features/`](.ai/features/) before adding new screens or services.
6. Update configuration, branding, and deployment files (see `build/`).

## License

Internal NIE project template.
