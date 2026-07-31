# 08 — OWASP Top 10 (2025) + API Security Top 10 Audit Checklist

Source of truth: <https://owasp.org/Top10/2025/> and OWASP API Security Top 10 2023 (refreshed for 2025 reading).

This checklist is the security gate for every PR that touches authentication, authorization, input handling, output, file uploads, external calls, or configuration.

## Web Application Top 10 (2025)

| # | Risk | Status (template baseline 2026.04.28) | Where it's enforced | Open gaps |
| --- | --- | --- | --- | --- |
| W-A01 | Broken Access Control | pass — `RequireAccessFunction` + access-function catalog + access-control admin UI | `BaseController`, `AccessControlController`, `AccessFunctionCatalog` | Per-record ownership checks (BOLA) — add to feature dossiers |
| W-A02 | Cryptographic Failures | partial — sessions in Valkey via TLS, password hashing handled by IDP. **Verify** TLS-only cookies, `Secure`/`HttpOnly`/`SameSite=Strict` everywhere | `Program.cs` antiforgery cookie config; Auth API session cookies | Audit cookie attributes; document TLS enforcement |
| W-A03 | Injection | pass — EF Core parameterizes; no `FromSqlRaw` allowed | `02-coding-standards-csharp.md` rule N-20 | — |
| W-A04 | Insecure Design | partial — STRIDE doc exists but not all features run threat model | `docs/security-model.md` | Run STRIDE per feature dossier |
| W-A05 | Security Misconfiguration | **gap** — security headers (CSP, HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy) not centrally configured | none currently | Add `SecurityHeadersMiddleware` + nginx config alignment |
| W-A06 | Vulnerable / outdated components | partial — Sentry/MailKit warnings present in build (NU1902) | `dotnet list package --vulnerable` + `pnpm audit` | Schedule monthly upgrade task |
| W-A07 | Identification & Authentication Failures | pass — Valkey session validation, configurable timeout, SSO supported | `SessionValidationMiddleware`, `AuthSessionService` | Verify session rotation on login, fixation guard |
| W-A08 | Software & Data Integrity Failures | partial — no SBOM publish in CI yet | `build/Jenkinsfile` | Add SBOM (`dotnet pack --include-source`, `pnpm sbom`) |
| W-A09 | Security Logging & Monitoring Failures | pass for entity changes (`AuditLog`); manual events via `IAuditLogger` | `MainDbContext.SaveChanges`, `IAuditLogger` | Verify login failure / access-denied / role-change events are wired |
| W-A10 | Server-Side Request Forgery (SSRF) | gap — outbound HTTP (MyInfo, Portal SSO) not allowlisted | `MyInfoService`, `PortalSsoService` | Add per-service URI allowlist + validate before send |

## API Security Top 10

| # | Risk | Status | Open gaps |
| --- | --- | --- | --- |
| API1 | Broken Object Level Authorization (BOLA) | partial — `RequireAccessFunction` is function-level only | Per-record ownership checks need a standard pattern (e.g. `IOwnedEntity` interface + filter) — open task |
| API2 | Broken Authentication | pass — `X-Session-Id` validated against Valkey; SSO dual-path | Confirm session expiry + rotation on login |
| API3 | Broken Object Property Level Authorization | partial — DTOs flatten what UI needs, but mass-assignment via `Edit` accepts whole DTO | Audit each `Edit` to ensure server-only fields aren't writable from request |
| API4 | Unrestricted Resource Consumption | partial — rate limiting registered globally; pagination caps not enforced | Cap PageSize at 100 (rule N-17) |
| API5 | Broken Function Level Authorization | pass | — |
| API6 | Unrestricted Access to Sensitive Business Flows | gap — anti-automation patterns not in template (e.g. captcha, throttle on submit-PO) | Document per-flow; add per-user throttling on financial flows |
| API7 | Server-Side Request Forgery | gap (see W-A10) | Allowlist outbound URIs |
| API8 | Security Misconfiguration | gap (see W-A05) | Security headers + CORS audit |
| API9 | Improper Inventory Management | gap — Swagger publicly served in production | Gate Swagger behind staging-only flag or auth |
| API10 | Unsafe Consumption of APIs | partial — MyInfo response not strictly schema-validated | Validate all 3rd-party JSON via typed DTO + checks |

## How to run a full audit

```bash
# .NET vulnerability scan
dotnet list src/backend/NieTemplate.sln package --vulnerable

# Frontend audit
cd src/frontend && pnpm audit

# Header smoke test
curl -I http://localhost:5002/api/Code/GetAll | grep -iE "strict-transport|content-security|x-content-type|x-frame|referrer-policy|permissions-policy"
```

## Open follow-up tasks (template baseline 2026.04.28)

- `[ ] W-A05 / API8 — add SecurityHeadersMiddleware (CSP, HSTS, X-Frame-Options, etc.) and align nginx config`
- `[ ] W-A10 / API7 — outbound URI allowlist for MyInfo + Portal SSO`
- `[ ] API1 — standardize per-record ownership pattern (`IOwnedEntity` + auth filter)`
- `[ ] API3 — audit every controller's `Edit` for accepted server-side fields`
- `[ ] API4 / N-17 — enforce max PageSize`
- `[ ] API9 — gate Swagger UI in production (auth or feature flag)`
- `[ ] API10 — typed validation of all external API responses`
- `[ ] W-A08 — SBOM generation in `build/Jenkinsfile``
- `[ ] W-A06 — monthly dependency upgrade task (recurring)`
