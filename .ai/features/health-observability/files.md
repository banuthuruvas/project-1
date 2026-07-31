# Health & Observability — File Map

## Owned files

| Path | Layer | Purpose |
| --- | --- | --- |
| `src/backend/API/Extensions/ObservabilityExtensions.cs` | Extension | `AddObservability(WebApplicationBuilder)` — early-return when `Sentry:Dsn` is empty; otherwise configures `UseSentry(...)` (DSN, Environment, TracesSampleRate, AttachStacktrace, AutoSessionTracking, UseOpenTelemetry) and OTel tracing with ASP.NET Core, HttpClient, EF Core instrumentations and the Sentry exporter |
| `src/backend/API/Middleware/CorrelationIdMiddleware.cs` | Middleware | Reads or generates `X-Correlation-Id`, attaches it to `HttpContext.TraceIdentifier`, sets the response header, and adds it to the log scope |

## Touched files

| Path | What it contains | Why must be touched |
| --- | --- | --- |
| `src/backend/API/Program.cs` line 35 | `builder.AddObservability()` | The single bootstrap call. Place it first in the builder pipeline so Sentry captures even early-init exceptions |
| `src/backend/API/Program.cs` lines 74-76 | `builder.Services.AddHealthChecks().AddNpgSql(...).AddRedis(...)` | Wires the Postgres and Valkey health probes into the `/health` pipeline |
| `src/backend/API/Program.cs` lines 240-247 | `app.MapHealthChecks("/health")`, `app.MapGet("/health/ready", ...)`, `app.MapGet("/health/live", ...)` | The three endpoints |
| `src/backend/API/Program.cs` line 209 | `app.UseMiddleware<CorrelationIdMiddleware>();` (FIRST middleware) | Required so every downstream log line carries the correlation id |
| `src/backend/API/Middleware/SessionValidationMiddleware.cs` line 99 | `/health` is in `skipPaths` | Probes must NOT require a session |
| `src/backend/Auth/Program.cs` lines 19-42 | Equivalent Sentry+OTel block for the Auth API | Auth traces are correlated with Main API traces via the same `Sentry:Environment` |
| `src/backend/API/appsettings.json` | `Sentry:Dsn`, `Sentry:Environment`, `Sentry:TracesSampleRate`, `Sentry:ServiceName` | Empty in template; populate per environment |

## External dependencies

| Package | Purpose |
| --- | --- |
| `Sentry.AspNetCore` | The Sentry SDK for ASP.NET Core (`UseSentry`) |
| `Sentry.OpenTelemetry` | The bridge `tracing.AddSentry()` and `options.UseOpenTelemetry()` |
| `OpenTelemetry.Extensions.Hosting` | `services.AddOpenTelemetry()` |
| `OpenTelemetry.Instrumentation.AspNetCore` | `tracing.AddAspNetCoreInstrumentation()` |
| `OpenTelemetry.Instrumentation.Http` | `tracing.AddHttpClientInstrumentation()` |
| `OpenTelemetry.Instrumentation.EntityFrameworkCore` | `tracing.AddEntityFrameworkCoreInstrumentation()` (Main API only) |
| `AspNetCore.HealthChecks.NpgSql` | `AddNpgSql(connectionString, name: "postgresql")` |
| `AspNetCore.HealthChecks.Redis` | `AddRedis(connectionString, name: "valkey")` |
