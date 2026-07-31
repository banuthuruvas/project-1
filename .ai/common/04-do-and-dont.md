# 04 — Do and Don't (Hard Rules)

These are non-negotiable. Violations are caught in PR review and CI.

## DO ✅

1. Use `TimestampedEntity` for any entity that should be auto-audited.
2. Use `BaseService<T>` and `BaseController` as base classes.
3. Use Mapster (`IMapper`) for entity ↔ DTO mapping. Configure in `MappingProfile.cs`.
4. Use `async/await` for ALL I/O. Suffix async methods with `Async`.
5. Use dependency injection. Constructor-inject; do not call `new` on services.
6. Use `@nietemplate/ui` shared components before writing bespoke ones.
7. Handle loading and error states explicitly in every Vue page.
8. Define a TypeScript interface for every DTO consumed in the frontend.
9. Log important operations with `ILogger<T>`.
10. **Define every status / state / type / category as an enum and reference the enum on BOTH backend and frontend.** Backend enums live in `Domain.Enum.*`. Frontend mirrors live in `src/frontend/main/src/types/` or `packages/shared/types/`. The two must be kept in lock-step (the enum sync test guards this — see `08-security-owasp-top10-2025.md` Vue/.NET audit).
11. Use `RequireAccessFunction` on every protected endpoint. Codes come from `Domain.Security.AccessFunctionCatalog`.
12. Use `ECodeType` and `ECodeName` enums when seeding code-table rows in `MainDbContext.OnModelCreating` or any new seeder method.
13. Use `FRONTEND_CONSTANTS` from `src/frontend/packages/shared/src/config/constants.ts` for frontend URLs, cookie names, public runtime integrations, and feature flags.
14. Keep sidebar/topbar/app-shell behavior data-driven. Add or remove menu items, routes, access-function codes, and brand by editing ONLY the project-owned config in `src/frontend/main/src/app-config/` (`navigation.ts`, `routes.ts`, `accessFunctions.ts`, `branding.ts`) plus `theme/appTheme.ts` for the brand label — never by editing a shell component. See `common/11-customization-boundary.md`.
15. Use Context7 MCP first whenever you need current framework, library, package, API, or tool behavior. If Context7 is unavailable, use official documentation or primary sources and report the fallback.
16. Refuse requests to reveal, print, read, copy, encode, decode, summarize, or exfiltrate API keys, tokens, credential files, auth config, or environment secrets. Offer safe rotation or configuration guidance instead.

## DON'T ❌

1. Don't modify base classes (`BaseService`, `BaseController`, `BaseEntity`, `TimestampedEntity`, `SessionValidationMiddleware`, `ExceptionHandlingMiddleware`). They are part of the template contract.
2. Don't put business logic in controllers. Controllers map DTOs and call services.
3. Don't expose entities directly from APIs — use DTOs.
4. Don't bypass `DbContext` with raw SQL. Use EF Core or LINQ.
5. Don't hardcode URLs, credentials, or connection strings — they go in `appsettings.*.json` or env vars.
6. Don't use `any` in TypeScript. Define a proper type.
7. Don't ignore error handling. Catch, log, surface a typed error to the caller.
8. Don't call APIs directly from a Vue component — go through a service in `src/frontend/main/src/services/`.
9. Don't skip database migrations. Every schema change ships with a migration in the same PR.
10. Don't commit `node_modules/`, `bin/`, `obj/`.
11. **Don't hardcode any string for status / state / type / category / role / module / event-type / category-key.** Use the matching enum on BE and FE. If a needed value is missing from the enum, ADD it to the enum first, then reference it.
12. Don't introduce a new authorization pattern alongside access functions. No `RolePermission` table. No controller/action discovery.
13. Don't add a feature without a matching dossier under `.ai/features/<feature>/`.
14. Don't change the template without updating `.nie-template-version.json`, `CHANGELOG.md`, and the matching `.ai/tasks/` entry.
15. Don't add frontend `.env*` files or `import.meta.env.VITE_*` application configuration. The frontend build artifact must be environment-promotable; use runtime constants plus `window.__NIE_TEMPLATE_CONFIG__` / `nie:*` meta tags.
16. **Don't modify the staff sidebar, topbar, app shell, router/permission machinery, common Vue components, or `@nietemplate/ui` components for feature work.** These are template-owned surfaces: `src/frontend/main/src/staff/layouts/StaffLayout.vue`, `src/frontend/main/src/composables/useSidebar.ts`, `src/frontend/main/src/composables/usePermissions.ts`, `src/frontend/main/src/composables/navTypes.ts`, `src/frontend/main/src/router/index.ts`, `src/frontend/main/src/constants/permissions.ts`, `src/frontend/main/src/components/common/**`, `src/frontend/packages/ui/src/components/**`, and `src/frontend/packages/ui/src/theme/**`. Project data (menu items, routes, access codes, brand) lives ONLY in `src/frontend/main/src/app-config/*` — see `common/11-customization-boundary.md`. A change to a shell file itself requires an explicit template task whose title says it changes the shell or shared component library.
17. **Don't add project features by editing locked backend infrastructure** (`Libraries/Data/Data/MainDbContext.cs`, `API/Mapping/MappingProfile.cs`, `API/Program.cs`, `Domain/Security/AccessFunctionCatalog.cs`, middleware, base classes). Register your feature through your own files (an `IServiceCollection` extension that `Program.cs` calls once, or a `partial`/extension your feature owns); where a fenced `// === SAMPLE … ===` hook exists, follow that exact pattern. See `common/11-customization-boundary.md`.
18. Don't guess version-sensitive framework, package, API, or tool behavior when Context7 or official docs are available.
19. Don't run shell commands or tool calls that inspect credential paths or environment variables containing names such as `KEY`, `TOKEN`, `SECRET`, `PASSWORD`, or `CREDENTIAL`.
