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
        var originalUri = new Uri(context.RedirectUri);
        context.RedirectUri = FixUpReturnUrlParameter(context.RedirectUri,
            context.Request.PathBase, context.Options.ReturnUrlParameter);

        logger.LogInformation("RedirectToLogin Request Header: {headers}, {redirectUri}",
            context.Request.Headers, context.RedirectUri);

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

    private string FixUpReturnUrlParameter(string redirectUri,
        string pathBase, string returnUrlParam)
    {
        if (string.IsNullOrWhiteSpace(redirectUri)
            || string.IsNullOrWhiteSpace(pathBase))
        {
            logger.LogInformation("Invalid data skipping URI fix. {redirectUri} {pathBase}",
                redirectUri, pathBase);
            return redirectUri;
        }

        var originalUri = new Uri(redirectUri);

        var qs = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(originalUri.Query);

        if (!qs.ContainsKey(returnUrlParam))
        {
            logger.LogInformation("{ReturnParam} not present in {QueryString} returning {RedirectUri}",
                returnUrlParam, qs, redirectUri);
            return redirectUri;
        }

        qs[returnUrlParam] = $"{pathBase}{qs[returnUrlParam]}";

        var pathOnly = $"{originalUri.Scheme}://{originalUri.Authority}{originalUri.AbsolutePath}";

        var updatedUri = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(pathOnly, qs);

        logger.LogInformation("Returning {UpdatedURI}, with {pathOnly}",
            updatedUri, pathOnly);

        return updatedUri;
    }
}