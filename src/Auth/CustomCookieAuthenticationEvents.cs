using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace TraefikForwardAuth.Auth;

public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
{
    private readonly ILogger<CustomCookieAuthenticationEvents> logger;

    public CustomCookieAuthenticationEvents(ILogger<CustomCookieAuthenticationEvents> logger)
    {
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

    private string BuildExternalRedirect(HttpRequest request)
    {
        var x = $"{request.Scheme}://{request.Host}";
        logger.LogInformation("External redirect: {uri}", x);
        return x;
    }
    private string GetAlteredRedirectUri(RedirectContext<CookieAuthenticationOptions> context)
    {
        var newRedirectUri = context.RedirectUri;
        logger.LogInformation("Current redirect {uri}", context.RedirectUri);
        // stuff the requesting host as "rediect" parameter for "/check" endpoint
        var redirect = new Uri(context.RedirectUri);
        var qs = HttpUtility.ParseQueryString(redirect.Query);
        var loginCheck = HttpUtility.UrlDecode(qs[context.Options.ReturnUrlParameter]);

        if (!string.IsNullOrWhiteSpace(loginCheck) && loginCheck.ToLower() == "/login/check")
        {
            var x = $"{loginCheck}?redirect=" + HttpUtility.UrlEncode(BuildExternalRedirect(context.Request));
            newRedirectUri = $"{redirect.Scheme}://{redirect.Authority}{redirect.LocalPath}"
                + "?returnUrl=" + HttpUtility.UrlEncode(x);
            logger.LogInformation("New redirect {uri}", newRedirectUri);
        }

        return newRedirectUri;
    }
}