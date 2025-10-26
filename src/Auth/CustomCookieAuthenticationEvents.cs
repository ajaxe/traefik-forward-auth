using System.ComponentModel.Design.Serialization;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using TraefikForwardAuth.Configuration;

namespace TraefikForwardAuth.Auth;

public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
{
    public const string ServiceTokenHeaderKey = "X-Service-Token";
    private readonly IAuthService authService;
    private readonly ILogger<CustomCookieAuthenticationEvents> logger;
    private readonly AppOptions options;

    public CustomCookieAuthenticationEvents(IAuthService authService,
        IOptions<AppOptions> options,
        ILogger<CustomCookieAuthenticationEvents> logger)
    {
        this.authService = authService;
        this.logger = logger;
        this.options = options.Value;
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        var originalUri = context.RedirectUri;
        context.RedirectUri = FixUpReturnUrlParameter(context.RedirectUri,
            context.Request.PathBase, context.Options.ReturnUrlParameter);

        if (context.Request.Headers.TryGetValue(ServiceTokenHeaderKey, out var v)
            && v.Any())
        {
            var serviceToken = v.First();
            context.RedirectUri = ApplyServiceTokenToRedirectUri(context.RedirectUri,
                context.Options.ReturnUrlParameter,
                serviceToken);
        }

        logger.LogInformation("RedirectToLogin Request Header: {@Headers}, {@OriginalUri} {@RedirectUri}",
            context.Request.Headers,
            originalUri,
            context.RedirectUri);

        return base.RedirectToLogin(context);
    }

    public override Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        return base.ValidatePrincipal(context);
    }
    public override Task SigningIn(CookieSigningInContext context)
    {
        var reqDomain = context.Request.Host.Value;
        var cookieDomain = options.GetAuthCookieDomain(reqDomain);

        if (!string.IsNullOrWhiteSpace(cookieDomain))
        {
            logger.LogInformation("Setting cookie {domain}", cookieDomain);
            context.CookieOptions.Domain = cookieDomain;
        }
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

        var updatedUri = RebuildUri(originalUri, qs);

        logger.LogInformation("FixUp: Returning {@OriginalRedirectUri} as {@UpdateUri}",
            redirectUri, updatedUri);

        return updatedUri;
    }
    /// <summary>
    /// Updates the redirect URI with the service token.
    /// </summary>
    /// <param name="redirectUri"></param>
    /// <param name="returnUrlParam"></param>
    /// <param name="serviceToken"></param>
    /// <returns></returns>
    internal string ApplyServiceTokenToRedirectUri(string redirectUri,
        string returnUrlParam, string? serviceToken)
    {
        if (string.IsNullOrWhiteSpace(serviceToken))
        {
            logger.LogInformation("ServiceToken is empty returning {RedirectUri}",
                redirectUri);
            return redirectUri;
        }
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            logger.LogInformation("RedirectUri is empty returning. {@ServiceToken}",
                serviceToken);
            return redirectUri;
        }
        var uri = new Uri(redirectUri);
        var qs = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

        if (!qs.ContainsKey(returnUrlParam))
        {
            logger.LogInformation("{ReturnParam} not present in {QueryString} returning {RedirectUri}",
                returnUrlParam, qs, redirectUri);
            return redirectUri;
        }
        qs[returnUrlParam] = $"/login/check?token={serviceToken}";

        var updateRedirectUri = RebuildUri(uri, qs);

        logger.LogInformation("Updated {@OriginalRedirectUri} to {@UpdatedRedirectUri}",
            redirectUri, updateRedirectUri);

        return updateRedirectUri;
    }

    internal static string RebuildUri(Uri uri, Dictionary<string, StringValues> queryString)
    {
        var updated = uri.IsAbsoluteUri
            ? $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}"
            : uri.OriginalString;

        return Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(updated, queryString);
    }
}