using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;
using TraefikForwardAuth.Configuration;

namespace TraefikForwardAuth.Auth;

public static class AuthenticationServiceExtensions
{
    public static AuthenticationBuilder AddDynamicCookieAuth(
            this IServiceCollection services,
            AppOptions appOptions,
            string appPrefix)
    {
        services.AddSingleton<ITicketStore, AppTicketStore>();

        services.AddScoped<CustomCookieAuthenticationEvents>();

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = "app_scheme.dynamic";
            options.DefaultChallengeScheme = "app_scheme.dynamic";
        })
        .AddPolicyScheme("app_scheme.dynamic", "Dynamic cookie scheme", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                string host = context.Request.Host.Host;
                foreach (var domain in appOptions.GetOrderedAuthDomains())
                {
                    if (host.Contains(domain, StringComparison.OrdinalIgnoreCase))
                    {
                        return appOptions.GetAuthSchemeName(domain);
                    }
                }

                throw new InvalidOperationException("Auth scheme not supported for invalid host: " + host);
            };
        });

        foreach (var domain in appOptions.GetOrderedAuthDomains())
        {
            var schemeName = appOptions.GetAuthSchemeName(domain);
            services.AddOptions<CookieAuthenticationOptions>(schemeName)
            .Configure<IDistributedCache, ILogger<AppTicketStore>>(
                (o, cache, logger) =>
                {
                    o.LoginPath = $"{appPrefix}/login";
                    o.ReturnUrlParameter = "returnUrl";
                    o.AccessDeniedPath = $"{appPrefix}/login/AccessDenied";
                    o.Cookie.Name = $".fwd-auth-{domain}";
                    o.Cookie.IsEssential = true;
                    o.EventsType = typeof(CustomCookieAuthenticationEvents);

                    o.SessionStore = new AppTicketStore(cache, TicketSerializer.Default, logger);
                });
            authBuilder.AddCookie(schemeName);
        }

        return authBuilder;
    }
}