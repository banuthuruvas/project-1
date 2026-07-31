using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;

namespace API.Extensions;

/// <summary>
/// Configures Sentry error monitoring with OpenTelemetry distributed tracing.
/// Set Sentry:Dsn in appsettings to enable; leave empty to disable.
/// </summary>
public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        var sentryDsn = builder.Configuration["Sentry:Dsn"];
        if (string.IsNullOrWhiteSpace(sentryDsn))
            return builder;

        var serviceName = builder.Configuration["Sentry:ServiceName"] ?? "nietemplate-api";
        var environment = builder.Configuration["Sentry:Environment"] ?? builder.Environment.EnvironmentName;
        var tracesSampleRate = double.TryParse(builder.Configuration["Sentry:TracesSampleRate"], out var rate) ? rate : 0.2;

        // Configure Sentry SDK
        builder.WebHost.UseSentry(options =>
        {
            options.Dsn = sentryDsn;
            options.Environment = environment;
            options.TracesSampleRate = tracesSampleRate;
            options.SendDefaultPii = false;
            options.AttachStacktrace = true;
            options.AutoSessionTracking = true;
            options.UseOpenTelemetry();
        });

        // Configure OpenTelemetry tracing with Sentry exporter
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSentry();
            });

        return builder;
    }
}
