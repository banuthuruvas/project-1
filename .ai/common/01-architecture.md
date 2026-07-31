# 01 — Architecture

```
src/backend/
├── API/                    # Main API service (port 5002)
│   ├── Controllers/        # API endpoints (extend BaseController)
│   ├── Middleware/         # Correlation, ETag, exception, session, user-roles
│   ├── Mapping/            # Mapster DTO mappings (MappingProfile.cs)
│   ├── Authorization/      # RequireAccessFunction attribute + handlers
│   ├── Extensions/         # ObservabilityExtensions, RateLimitingExtensions, TickerQExtensions, DatabaseSeeder
│   ├── Jobs/               # TickerQ background jobs
│   └── Program.cs          # Service registration & startup
├── Auth/                   # Auth API service (port 5001)
│   ├── Controllers/        # Login, logout, sessions, SSO callback
│   ├── Models/             # Login/SSO DTOs
│   └── Services/           # AuthSessionService, PortalSsoService
└── Libraries/
    ├── Domain/Models/      # Entity classes (extend TimestampedEntity for auditable types)
    ├── Domain/Dto/         # Data transfer objects (no nav properties)
    ├── Domain/Enum/        # ALL status / type enums (single source of truth)
    ├── Domain/Security/    # AccessFunctionCatalog (canonical access-function codes + role bundles)
    ├── Data/Data/          # MainDbContext (auto-audit + auto-timestamps)
    ├── Data/Migrations/    # EF Core migrations
    ├── Services/Services/  # Business logic (extend BaseService<T>)
    └── Shared/             # IUserContextService, helpers, settings types

src/frontend/               # pnpm monorepo
├── main/                   # Main user application (port 8001)
├── auth/                   # Login application (port 8002)
└── packages/
    ├── ui/                 # @nietemplate/ui — reusable Vue components + theme runtime
    └── shared/             # @nietemplate/shared — utilities, types, composables

build/                      # Docker + Jenkins deployment
tests/                      # Playwright API + E2E tests
tools/                      # Versioning + Portal SSO tooling
.ai/                        # AI agent instructions (this folder)
```

## Auth boundary

- **Auth API (5001)**: only service that mints sessions. Owns login, logout, refresh, SSO start/callback. Validates against IDP + Valkey. No DB writes outside session metadata.
- **Main API (5002)**: validates `X-Session-Id` header via `SessionValidationMiddleware`. Looks up user context from Valkey, populates `BaseController.UserId / UserRoles / UserAccessFunctions / IsAdmin`.

## Automatic behaviors (see `04-do-and-dont.md` before changing)

- **Audit logging**: `MainDbContext.SaveChanges` automatically captures create/update/delete for any `TimestampedEntity` subclass. Manual events go through `IAuditLogger`.
- **Timestamps**: `CreatedOn`, `UpdatedOn`, `CreatedBy`, `UpdatedBy` are set automatically. Never set them in service or controller code.
- **Session validation**: every request through Main API except Swagger / health / favicon is validated.
- **Migrations apply on startup**: `Program.cs` runs `Database.Migrate()` on boot. Do not deploy a migration that requires manual SQL.
