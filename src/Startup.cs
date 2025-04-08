
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TraefikForwardAuth.Auth;
using TraefikForwardAuth.Configuration;
using TraefikForwardAuth.Helpers;

namespace TraefikForwardAuth;

public class Startup
{
    const string EnvVarPrefix = "APP_";
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

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardLimit = 2;
            options.KnownProxies.Clear();
            options.AllowedHosts.Clear();
            options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All;
        });

        services.AddOptions();
        services.AddHealthChecks();

        var appOptions = new AppOptions();
        Configuration.GetSection(AppOptions.SectionName)
            .Bind(appOptions);
        services.Configure<AppOptions>(
            Configuration.GetSection(AppOptions.SectionName)
        );

        services.AddDbContext<AppDbContext>(
            o => o.UseMongoDB(appOptions.MongoDbConnection, appOptions.DatabaseName)
        );

        services.AddTransient<IHostedApplicationService, HostedApplicationService>();
        services.AddTransient<IAuthService, AppAuthService>();

        if (Environment.IsProduction())
        {
            services.AddDataProtection()
                .SetApplicationName("TraefikForwardAuth")
                .PersistKeysToFileSystem(new DirectoryInfo("/dpapi-keys/"));
        }

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.LoginPath = $"{appPrefix}/login";
                o.ReturnUrlParameter = "returnUrl";
                o.AccessDeniedPath = $"{appPrefix}/login/AccessDenied";
                o.Cookie.Name = ".fwd-auth-custom";
                o.Cookie.IsEssential = true;
                o.EventsType = typeof(CustomCookieAuthenticationEvents);
            });

        services.AddScoped<CustomCookieAuthenticationEvents>();
        services.AddControllersWithViews();
        services.AddHttpContextAccessor();
        services.AddExceptionHandler<GlobalExceptionHandler>();
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

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            endpoints.MapHealthChecks("/healthcheck");
        });
    }
}