using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace TraefikForwardAuth.Auth;

public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
{
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        var x = context.Request.Query["returnUrl"].FirstOrDefault();
        var _ = context.RedirectUri;
        /* if (!string.IsNullOrWhiteSpace(x))
        {
            context.RedirectUri = "/login?returnUrl=" + x;
        } */
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