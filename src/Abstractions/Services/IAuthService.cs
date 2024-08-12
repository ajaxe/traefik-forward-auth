using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Authentication;

namespace TraefikForwardAuth.Abstractions.Services;

public interface IAuthService
{
    Task<string> AuthCheck(AuthCheckData authCheckData);
    Task<AuthenticationResult> Authenticate(string username, string password);

    Task<bool> ValidatePrincipalRefererUrl(IEnumerable<Claim> claims, string? referer);
}

public class AuthenticationResult
{
    public ClaimsPrincipal Principal { get; set; }
    public string UserId { get; set; }
    public bool Success { get; set; }
    public AuthenticationProperties AuthProperties { get; internal set; }
}

public class AuthCheckData
{
    public string ServiceToken { get; internal set; }
    public IEnumerable<Claim> Claims { get; internal set; }
}