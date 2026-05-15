using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using TraefikForwardAuth.Auth;
using TraefikForwardAuth.Configuration;
using TraefikForwardAuth.Helpers;

namespace TraefikForwardAuth;

public class Startup
{
    private const string AppName = "TraefikForwardAuth";
    public const string EnvVarPrefix = "APP_";
    string appPrefix = System.Environment.GetEnvironmentVariable($"{EnvVarPrefix}AppPathPrefix") ?? string.Empty;
    public Startup(ConfigurationManager configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;
    }

    public ConfigurationManager Configuration { get; }
    public IWebHostEnvironment Environment { get; }
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSerilog((s, lc) => lc.ReadFrom.Configuration(Configuration));

        AddOpenTelemetry(services);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardLimit = 2;
            options.KnownProxies.Clear();
            options.AllowedHosts.Clear();
            options.ForwardedHeaders = ForwardedHeaders.All;
        });

        services.AddOptions();
        services.AddHealthChecks();

        var appOptions = new AppOptions();
        Configuration.GetSection(AppOptions.SectionName)
            .Bind(appOptions);
        services.Configure<AppOptions>(
            Configuration.GetSection(AppOptions.SectionName)
        );

        services.AddCors(opts =>
        {
            opts.AddDefaultPolicy(policy =>
            {
                var d = appOptions.GetOrderedAuthDomains();
                if (d.Count() > 0)
                    policy.WithOrigins(d.Select(s => $"https://*{s}").ToArray())
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetIsOriginAllowedToAllowWildcardSubdomains();
            });
        });

        services.AddSingleton<IMongoClient>(sp =>
        {
            var clientSettings = MongoClientSettings.FromConnectionString(appOptions.MongoDbConnection);
            clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
            return new MongoClient(clientSettings);
        });
        services.AddDbContext<AppDbContext>(
            (sp, o) => o.UseMongoDB(sp.GetRequiredService<IMongoClient>(), appOptions.DatabaseName)
        );

        services.AddStackExchangeRedisCache(o =>
        {
            o.InstanceName = $"{AppName}:{Environment.EnvironmentName}:";
            o.Configuration = Configuration.GetConnectionString("cache");
        });

        services.AddTransient<IHostedApplicationService, HostedApplicationService>();
        services.AddTransient<IAuthService, AppAuthService>();

        if (Environment.IsProduction())
        {
            services.AddDataProtection()
                .SetApplicationName(AppName)
                .PersistKeysToFileSystem(new DirectoryInfo("/dpapi-keys/"));
        }

        services.AddDynamicCookieAuth(appOptions, appPrefix);

        services.AddControllersWithViews();
        services.AddHttpContextAccessor();
        services.AddExceptionHandler<GlobalExceptionHandler>();
    }

    private void AddOpenTelemetry(IServiceCollection services)
    {
        var appName = "TraefikForwardAuth";
        var otelEndpoint = Configuration["OTLP_ENDPOINT_URL"]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(otelEndpoint))
        {
            Log.Warning("OTLP_ENDPOINT_URL is not set, OpenTelemetry will not be configured.");
            return;
        }

        var otel = services.AddOpenTelemetry()
        .ConfigureResource(resource =>
        {
            resource.AddService(serviceName: ActivitySources.AppName);
            var globalOpenTelemetryAttributes = new List<KeyValuePair<string, object>>
            {
                new ("env", Environment.EnvironmentName),
                new ("service.name", ActivitySources.AppName),
                new ("service.version", "1.0.0"),
                new ("service.instanceId", System.Environment.MachineName),
            };
            resource.AddAttributes(globalOpenTelemetryAttributes);
        })
        .WithMetrics(metrics => metrics
            .AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(otelEndpoint);
            })
            // Metrics provider from OpenTelemetry
            .AddAspNetCoreInstrumentation()
            .AddMeter(ActivitySources.AppName)
            // Metrics provides by ASP.NET Core in .NET 8
            .AddMeter("Microsoft.AspNetCore.Hosting")
            .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
            .AddPrometheusExporter())
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(ActivitySources.AppName)
                .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources")
                .AddOtlpExporter(otlpOptions =>
                {
                    otlpOptions.Endpoint = new Uri(otelEndpoint);
                })
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.EnrichWithIDbCommand = (activity, command) =>
                    {
                        var stateDisplayName = $"{command.CommandType} main";
                        activity.DisplayName = stateDisplayName;
                        activity.SetTag("db.name", stateDisplayName);
                    };
                });

            if (Environment.IsDevelopment())
                tracing.AddConsoleExporter();
        });
    }

    public void Configure(IApplicationBuilder app)
    {

        if (!string.IsNullOrWhiteSpace(appPrefix))
        {
            app.Use((context, next) =>
            {
                context.Request.PathBase = appPrefix;
                return next();
            });
        }

        app.Use((context, next) =>
        {
            // use protocol as forwarded by reverse proxy
            // https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-5.0#when-it-isnt-possible-to-add-forwarded-headers-and-all-requests-are-secure-1
            var scheme = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(scheme))
                context.Request.Scheme = scheme;
            return next();
        });

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.All
        });

        app.UseStaticFiles();

        app.UseRouting();

        app.UseCors();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            endpoints.MapHealthChecks("/healthcheck").AllowAnonymous();
        });
    }
}
