using Amazon.Runtime;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace TraefikForwardAuth.Auth;

public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
{
    private readonly IAuthService authService;
    private readonly ILogger<CustomCookieAuthenticationEvents> logger;

    public CustomCookieAuthenticationEvents(IAuthService authService,
        ILogger<CustomCookieAuthenticationEvents> logger)
    {
        this.authService = authService;
        this.logger = logger;
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        logger.LogInformation("Incoming Request Header: {headers}", context.Request.Headers);
        return base.RedirectToLogin(context);
    }
    public override Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        return base.ValidatePrincipal(context);
    }
    public override Task SigningIn(CookieSigningInContext context)
    {
        return base.SigningIn(context);
    }
}