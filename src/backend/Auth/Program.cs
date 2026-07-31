using Auth.Controllers;
using Auth.Models;
using Auth.Services;
using Microsoft.OpenApi;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;
using StackExchange.Redis;

namespace Auth;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;

        var sentryDsn = configuration["Sentry:Dsn"];
        if (!string.IsNullOrWhiteSpace(sentryDsn))
        {
            builder.WebHost.UseSentry(options =>
            {
                options.Dsn = sentryDsn;
                options.Environment = configuration["Sentry:Environment"] ?? builder.Environment.EnvironmentName;
                options.TracesSampleRate = double.TryParse(configuration["Sentry:TracesSampleRate"], out var rate) ? rate : 0.2;
                options.SendDefaultPii = false;
                options.AttachStacktrace = true;
                options.AutoSessionTracking = true;
                options.UseOpenTelemetry();
            });

            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(configuration["Sentry:ServiceName"] ?? "nietemplate-auth"))
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddSentry();
                });
        }

        // Auth API connects to IDP + Valkey only. No database, no service-to-service calls.
        // Role/permission resolution happens in the Main API, fetched by the frontend after login.

        builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var connectionString = builder.Configuration["Valkey:ConnectionString"]
                ?? throw new InvalidOperationException("Valkey:ConnectionString configuration is required.");
            return ConnectionMultiplexer.Connect(connectionString);
        });

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            var valkeyConnectionString = builder.Configuration["Valkey:ConnectionString"]
                ?? throw new InvalidOperationException("Valkey:ConnectionString configuration is required.");
            options.ConfigurationOptions = new ConfigurationOptions
            {
                EndPoints = { valkeyConnectionString },
                AbortOnConnectFail = false
            };
            options.InstanceName = builder.Configuration["Valkey:InstanceName"];
        });

        builder.Services.AddCors(options =>
        {
            var allowedOrigins = configuration.GetSection("AllowedCORSOrigin").Get<string[]>()
                ?? throw new InvalidOperationException("AllowedCORSOrigin configuration is required.");
            options.AddPolicy("AllowSpecificOrigin",
                policy => policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
        });

        builder.Services.AddHttpClient<AuthController>();
        builder.Services.Configure<PortalSsoOptions>(configuration.GetSection(PortalSsoOptions.SectionName));
        builder.Services.AddScoped<IAuthSessionService, AuthSessionService>();
        builder.Services.AddScoped<IPortalSsoService, PortalSsoService>();
        builder.Services.AddControllers();
        builder.Services.AddHealthChecks();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "NieTemplate Auth API", Version = "v1" });

            c.AddSecurityDefinition("Session", new OpenApiSecurityScheme
            {
                Description = "Session-based authentication using session ID",
                Name = "sessionToken",
                In = ParameterLocation.Query,
                Type = SecuritySchemeType.ApiKey
            });

            c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Session"),
                    new List<string>()
                }
            });
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "NieTemplate Auth API V1");
            });
        }

        app.UseCors("AllowSpecificOrigin");
        app.MapHealthChecks("/health");
        app.MapGet("/health/live", () => Results.Ok("ok"));
        app.MapControllers();
        app.Run();
    }
}
