using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using TraefikForwardAuth.Configuration;

namespace TraefikForwardAuth.Auth;

public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationOptions>
{
    public const string SchemeName = "BasicAuth";
    private readonly AppOptions appOptions;
    public BasicAuthenticationHandler(IOptionsMonitor<BasicAuthenticationOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, IOptions<AppOptions> appOptions)
        : base(options, logger, encoder)
    {
        this.appOptions = appOptions.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (this.Request.Path.ToString().ToLower() == "/healthcheck")
            return Task.FromResult(AuthenticateResult.NoResult());

        if (string.IsNullOrWhiteSpace(appOptions.Username)
            || string.IsNullOrWhiteSpace(appOptions.Password))
        {
            Logger.LogInformation("Missing configured username/password. {path}", this.Request.Path);
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var header = Request.Headers["Authorization"];
        if (!header.Any())
        {
            Logger.LogInformation("Missing authorization header. {path}", this.Request.Path);
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            if (!AuthenticationHeaderValue.TryParse(header.First()!.ToString(), out var authHeader)
                && authHeader?.Scheme?.ToLower() != "basic")
            {
                Logger.LogInformation("Invalid auth scheme in header");
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var credentialBytes = Convert.FromBase64String(authHeader.Parameter ?? string.Empty);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
            var username = credentials[0];
            var password = credentials[1];

            if (username != appOptions.Username || password != appOptions.Password)
            {
                Logger.LogError("Incorrect username/password.");
                return Task.FromResult(AuthenticateResult.Fail("Incorrect username/password."));
            }

            Logger.LogInformation("Forwarded username/password: {username} {password}. {path}",
                username, password, this.Request.Path);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "0"),
                new Claim(ClaimTypes.Name, username),
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);

            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            Logger.LogInformation("{data}", new
            {
                XForwardedMethod = Request.Headers["X-Forwarded-Method"],
                XForwardedHost = Request.Headers["X-Forwarded-Host"],
                XForwardedUri = Request.Headers["X-Forwarded-Uri"],
            });

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch
        {
            Logger.LogError("Error Occured.Authorization failed.");
            return Task.FromResult(AuthenticateResult.Fail("Error Occured.Authorization failed."));
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        this.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"forwarded-auth\"");
        this.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        Logger.LogInformation("Setting response headers for challenge. {path}", this.Request.Path);
        return Task.CompletedTask;
    }
}

public class BasicAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = "BasicAuth";
    public BasicAuthenticationOptions() { }
}