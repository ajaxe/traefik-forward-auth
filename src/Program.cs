using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TraefikForwardAuth.Auth;
using TraefikForwardAuth.Configuration;

const string EnvVarPrefix = "APP_";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((s, lc) => lc.ReadFrom.Configuration(builder.Configuration));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardLimit = 2;
    options.KnownProxies.Clear();
    options.AllowedHosts.Clear();
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All;
});

builder.Services.AddOptions();
builder.Configuration
    .AddJsonFile("secrets.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables(prefix: EnvVarPrefix);

builder.Services.AddHealthChecks();

var appOptions = new AppOptions();
builder.Configuration.GetSection(AppOptions.SectionName)
    .Bind(appOptions);
builder.Services.Configure<AppOptions>(
    builder.Configuration.GetSection(AppOptions.SectionName)
);

builder.Services.AddDbContext<AppDbContext>(
    o => o.UseMongoDB(appOptions.MongoDbConnection, appOptions.DatabaseName)
);
builder.Services.AddTransient<IHostedApplicationService, HostedApplicationService>();
builder.Services.AddTransient<IAuthService, AppAuthService>();

if (builder.Environment.IsProduction())
{
    builder.Services.AddDataProtection()
        .SetApplicationName("TraefikForwardAuth")
        .PersistKeysToFileSystem(new DirectoryInfo("/dpapi-keys/"));
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    //.AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(BasicAuthenticationOptions.SchemeName, null)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        o.SlidingExpiration = true;
        o.ReturnUrlParameter = "returnUrl";
        o.AccessDeniedPath = "/login/AccessDenied";
        o.Cookie.Name = ".fwd-auth-custom";
        o.Cookie.IsEssential = true;
        o.EventsType = typeof(CustomCookieAuthenticationEvents);
    });

// Add services to the container.
builder.Services.AddScoped<CustomCookieAuthenticationEvents>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.Use((context, next) =>
{
    // use protocol as forwarded by reverse proxy
    // https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-5.0#when-it-isnt-possible-to-add-forwarded-headers-and-all-requests-are-secure-1
    var scheme = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? string.Empty;
    if (!string.IsNullOrWhiteSpace(scheme))
        context.Request.Scheme = scheme;
    return next();
});

app.UseForwardedHeaders();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHealthChecks("/healthcheck");

app.Run();
