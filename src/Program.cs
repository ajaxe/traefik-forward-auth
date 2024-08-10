using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TraefikForwardAuth.Auth;
using TraefikForwardAuth.Configuration;

const string EnvVarPrefix = "APP_";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((s, lc) => lc.ReadFrom.Configuration(builder.Configuration));

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

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHealthChecks("/healthcheck");

app.Run();
