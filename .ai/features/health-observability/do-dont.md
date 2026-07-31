# Health & Observability — Do and Don't

## DO ✅

1. **DO** keep three distinct health endpoints. `/health` exercises dependencies and is for ALB/uptime tools; `/health/ready` is for "ready to receive traffic" gates; `/health/live` is for liveness only. Conflating them defeats Kubernetes probe semantics.
2. **DO** add new dependency probes via `services.AddHealthChecks().AddXxx(...)` (the `AspNetCore.HealthChecks.*` package family covers Postgres, Redis/Valkey, RabbitMQ, S3, OpenAI, etc.). Each new probe registers a name; the `/health` JSON response shows pass/fail per name.
3. **DO** call `builder.AddObservability()` as early as possible in `Program.cs` (currently line 35, before any other DI registrations). Sentry needs to attach before exceptions can occur during config / DI build.
4. **DO** populate `Sentry:Dsn` per environment via secrets / env vars. The base `appsettings.json` ships empty so dev environments are quiet by default.
5. **DO** set `Sentry:TracesSampleRate` lower in production (e.g. `0.1` or `0.05`) than in staging (`0.5`) than in dev (`1.0`). The default in code is `0.2`.
6. **DO** set `Sentry:Environment` to a clean string (`production`, `staging`, `dev-personal`) so the dashboard filters work cleanly.
7. **DO** trust the OTel ASP.NET / HttpClient / EF Core instrumentations to capture spans automatically. Avoid manual `Activity` creation unless you have a span the auto-instrumentation can't see.
8. **DO** use `AddObservability` as the ONLY observability boot site. Adding a second `UseSentry(...)` later in the pipeline produces duplicate captures.
9. **DO** keep `SessionValidationMiddleware.skipPaths` containing `/health` — uptime probes from the load balancer typically don't carry a session header.
10. **DO** include the correlation id in every log line. The `CorrelationIdMiddleware` writes it into `HttpContext.TraceIdentifier`, which the default ASP.NET log scope picks up automatically.

## DON'T ❌

1. **DON'T** put database calls inside `/health/live`. The liveness probe must succeed even when the DB is down — otherwise Kubernetes restarts a pod that's actually fine.
2. **DON'T** put database calls inside `/health/ready` either, beyond a trivial check. Slow probes cause cascade failures during outages. Use `/health` (the full pipeline) for dependency exercise.
3. **DON'T** set `Sentry:SendDefaultPii = true`. The current code explicitly sets `false`. Sending PII to Sentry violates DPA constraints.
4. **DON'T** set `Sentry:TracesSampleRate = 1.0` in production. Every traced request uploads spans; at scale this is expensive and noisy.
5. **DON'T** swallow exceptions silently — Sentry only captures unhandled exceptions plus those reported via `ILogger.LogError(ex, ...)`. A try/catch that does nothing hides errors from the dashboard.
6. **DON'T** add custom middleware before `CorrelationIdMiddleware`. The correlation id must be available to all later middleware (especially exception handling).
7. **DON'T** disable `app.UseGlobalExceptionHandling()` (line 212). It feeds errors into Sentry via `ILogger.LogError` AND returns a sanitized `ApiResponse` to the client.
8. **DON'T** use Application Insights or Datadog APM concurrently with Sentry+OTel. Pick one APM. Mixing causes duplicate spans and confusing dashboards.
9. **DON'T** rename the health endpoint paths without updating both the load balancer config AND the `SessionValidationMiddleware` skip list (line 99). Mismatch breaks probes.
10. **DON'T** assume `AddObservability` runs in dev — the function `return builder` early when `Sentry:Dsn` is empty. If you need OTel tracing in dev WITHOUT Sentry, set a fake DSN value and route to a local OTLP collector instead.
