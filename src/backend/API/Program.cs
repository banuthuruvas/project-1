using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.OpenApi;
using API.Authorization;
using API.Extensions;
using API.Mapping;
using API.Middleware;
using Data.Data;
using Domain.Services;
using Domain.Services.Code;
using Domain.Services.Document;
using Domain.Services.FileStorage;
using Mapster;
using MapsterMapper;
using Domain.Services.Vendor;
using Domain.Services.CatalogItem;
using Domain.Services.PurchaseOrder;
using Domain.Services.PurchaseOrderDocument;
using Domain.Services.Workflow;
using Services.Services;
using Shared.Helpers;
using Shared.Interfaces;
using Shared.Models;
using StackExchange.Redis;

namespace API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;

        // Add observability (Sentry + OpenTelemetry)
        builder.AddObservability();

        // Add HttpContextAccessor for user context
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUserContextService, UserContextService>();

        // Add database context
        builder.Services.AddDbContext<MainDbContext>(options =>
            options
                .UseNpgsql(
                    configuration.GetConnectionString("MainDbConnection"),
                    b => b.MigrationsAssembly("Data"))
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

        // Resolve generic DbContext to MainDbContext so generic action filters
        // (e.g. OwnedEntityActionFilter<TEntity>) can use it without coupling to MainDbContext.
        builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<MainDbContext>());

        // Bind SecurityHeadersOptions so SecurityHeadersMiddleware can apply CSP/HSTS/etc.
        builder.Services.Configure<API.Middleware.SecurityHeadersOptions>(
            configuration.GetSection(API.Middleware.SecurityHeadersOptions.SectionName));

        // Ensure directory exists for file uploads
        var uploadPath = builder.Configuration["FileStorage:BasePath"];
        if (!string.IsNullOrEmpty(uploadPath) && !Directory.Exists(uploadPath))
            Directory.CreateDirectory(uploadPath);

        // Add Valkey connection
        builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var connectionString = builder.Configuration["Valkey:ConnectionString"]
                ?? throw new InvalidOperationException("Valkey connection string is not configured.");
            return ConnectionMultiplexer.Connect(connectionString);
        });

        // Add distributed cache for session (required for IDistributedCache)
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            var valkeyConnectionString = builder.Configuration["Valkey:ConnectionString"]
                ?? throw new InvalidOperationException("Valkey connection string is not configured.");
            options.ConfigurationOptions = new ConfigurationOptions
            {
                EndPoints = { valkeyConnectionString },
                AbortOnConnectFail = false
            };
            options.InstanceName = builder.Configuration["Valkey:InstanceName"];
        });

        // Add health checks
        builder.Services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("MainDbConnection")!, name: "postgresql")
            .AddRedis(configuration["Valkey:ConnectionString"]!, name: "valkey");

        // Add services to the container
        builder.Services.AddScoped<ICodeService, CodeService>();
        builder.Services.AddScoped<IDocumentService, DocumentService>();
        builder.Services.AddScoped<IFileStorageService, FileStorageService>();

        // Add procurement services
        builder.Services.AddScoped<IVendorService, VendorService>();
        builder.Services.AddScoped<ICatalogItemService, CatalogItemService>();
        builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        builder.Services.AddScoped<IPurchaseOrderDocumentService, PurchaseOrderDocumentService>();

        // Add workflow service
        builder.Services.AddScoped<IWorkflowService, WorkflowService>();

        // Add optional feature services when their Copier-gated files are present.
        RegisterOptionalScopedService(
            builder.Services,
            "Domain.Services.Chat.IChatService, Services",
            "Domain.Services.Chat.ChatService, Services");

        // Add audit and role management services
        builder.Services.AddScoped<IAuditLogService, AuditLogService>();
        builder.Services.AddScoped<IAuditLogger, AuditLogger>();
        builder.Services.AddScoped<IAccessFunctionService, AccessFunctionService>();
        builder.Services.AddScoped<IRoleService, RoleService>();
        builder.Services.AddScoped<IUserRoleService, UserRoleService>();

        // Add email service
        builder.Services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        builder.Services.AddScoped<IEmailService>(sp =>
            new EmailService(
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailSettings>>(),
                sp.GetRequiredService<ILogger<EmailService>>(),
                builder.Environment.ContentRootPath));

        // Add push notification service (OneSignal)
        builder.Services.Configure<OneSignalSettings>(configuration.GetSection("OneSignal"));
        builder.Services.AddHttpClient<IPushNotificationService, OneSignalPushNotificationService>();

        // Add optional MyInfo service when the Copier-gated files are present.
        RegisterOptionalMyInfoService(builder.Services);

        // Configure Mapster
        MappingConfig.RegisterMappings();
        builder.Services.AddSingleton(TypeAdapterConfig.GlobalSettings);
        builder.Services.AddScoped<IMapper, ServiceMapper>();

        // Add CORS policy
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigin",
                policy => policy
                    .WithOrigins(configuration.GetSection("AllowedCORSOrigin").Get<string[]>() ?? Array.Empty<string>())
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
        });

        // Add rate limiting
        builder.Services.AddRateLimiting(configuration);

        // Add response caching
        builder.Services.AddResponseCaching();

        // Add anti-forgery (CSRF protection for session-based auth)
        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "XSRF-TOKEN";
            options.Cookie.HttpOnly = false; // Frontend needs to read the cookie
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        // Add API versioning
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Add session validation
        builder.Services.AddSessionValidation(configuration);

        // Add TickerQ for background job processing
        builder.Services.AddTickerQServices(configuration, builder.Environment);

        builder.Services.AddControllers();

        // Configure Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "NieTemplate API",
                Version = "v1",
                Description = "NieTemplate API - A template for building .NET applications",
                Contact = new OpenApiContact
                {
                    Name = "NIE Development Team"
                }
            });

            // Configure Swagger to use Session Authentication
            c.AddSecurityDefinition("Session", new OpenApiSecurityScheme
            {
                Description = "Session-based authentication using session ID in header",
                Name = "X-Session-Id",
                In = ParameterLocation.Header,
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

        // Handle "dotnet run -- seed" for database seeding
        if (args.Contains("seed", StringComparer.OrdinalIgnoreCase))
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            await DatabaseSeeder.SeedAsync(db);
            Console.WriteLine("Database seeded successfully.");
            return;
        }

        // Correlation ID middleware (must be first)
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Use global exception handling middleware
        app.UseGlobalExceptionHandling();

        // Rate limiting
        app.UseRateLimiter();

        // Configure the HTTP request pipeline
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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "NieTemplate API V1");
            });
        }

        // Enable CORS before other middleware
        app.UseCors("AllowSpecificOrigin");

        // Security response headers (CSP / HSTS / X-Frame-Options / etc.)
        app.UseSecurityHeaders();

        // Response caching + ETag support
        app.UseResponseCaching();
        app.UseMiddleware<ETagMiddleware>();

        // Map health check endpoints (used by uptime monitoring / Sentry Crons)
        app.MapHealthChecks("/health");
        app.MapGet("/health/ready", () => Results.Ok(new
        {
            status = "healthy",
            service = "nietemplate-api",
            timestamp = DateTimeHelper.Now
        }));
        app.MapGet("/health/live", () => Results.Ok("ok"));

        // Configure TickerQ dashboard and job host before session validation so the dashboard can manage its own auth.
        app.UseTickerQServices();

        // Use session validation middleware
        app.UseSessionValidation();

        // Use authorization middleware
        app.UseAuthorization();

        app.MapControllers();

        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<MainDbContext>();

            context.Database.Migrate();
        }

        await app.RunAsync();
    }

    private static void RegisterOptionalScopedService(
        IServiceCollection services,
        string serviceTypeName,
        string implementationTypeName)
    {
        var serviceType = Type.GetType(serviceTypeName, throwOnError: false);
        var implementationType = Type.GetType(implementationTypeName, throwOnError: false);
        if (serviceType is null || implementationType is null)
        {
            return;
        }

        services.AddScoped(serviceType, implementationType);
    }

    private static void RegisterOptionalMyInfoService(IServiceCollection services)
    {
        var serviceType = Type.GetType("Services.Services.MyInfo.IMyInfoService, Services", throwOnError: false);
        var implementationType = Type.GetType("Services.Services.MyInfo.MyInfoService, Services", throwOnError: false);
        if (serviceType is null || implementationType is null)
        {
            return;
        }

        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddScoped(serviceType, serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("MyInfo");
            return ActivatorUtilities.CreateInstance(serviceProvider, implementationType, httpClient);
        });
    }
}
