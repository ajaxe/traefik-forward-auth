using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using TraefikForwardAuth.Auth;
using TraefikForwardAuth.Configuration;

const string EnvVarPrefix = "APP_";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions();
builder.Configuration
    .AddEnvironmentVariables(prefix: EnvVarPrefix);

builder.Services.AddHealthChecks();

var appOptions = new AppOptions();
builder.Configuration.GetSection(AppOptions.SectionName)
    .Bind(appOptions);
builder.Services.Configure<AppOptions>(
    builder.Configuration.GetSection(AppOptions.SectionName)
);

if (builder.Environment.IsProduction())
{
    builder.Services.AddDataProtection()
        //.SetApplicationName("TraefikForwardAuth")
        .PersistKeysToFileSystem(new DirectoryInfo("/dpapi-keys/"));
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    //.AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(BasicAuthenticationOptions.SchemeName, null)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.ExpireTimeSpan = TimeSpan.FromMinutes(1);
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
