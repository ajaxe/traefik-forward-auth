using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Authentication;

namespace TraefikForwardAuth.Abstractions.Services;

public interface IAuthService
{
    Task<string> AuthCheck(AuthCheckData authCheckData);
    Task<AuthenticationResult> Authenticate(AuthenticateData authenticateData);

    Task<bool> ValidatePrincipalRefererUrl(IEnumerable<Claim> claims, string? referer);
}

public class AuthenticationResult
{
    public ClaimsPrincipal Principal { get; set; }
    public string UserId { get; set; }
    public bool Success { get; set; }
    public AuthenticationProperties AuthProperties { get; internal set; }

    public static AuthenticationResult Fail()
    {
        return new AuthenticationResult
        {
            Success = false,
        };
    }
}

public class AuthCheckData
{
    public string ServiceToken { get; internal set; } = default!;
    public IEnumerable<Claim> Claims { get; internal set; } = default!;
}

public class AuthenticateData
{
    public string Username { get; internal set; } = default!;
    public string Password { get; internal set; } = default!;
    public string RequestingDomain { get; internal set; } = default!;
}
