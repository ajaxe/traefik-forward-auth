using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Authentication;

namespace TraefikForwardAuth.Abstractions.Services;

public interface IAuthService
{
    Task<string> AuthCheck(AuthCheckData authCheckData);
    Task<AuthenticationResult> Authenticate(string username, string password);
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
    public Guid ServiceToken { get; internal set; }
    public IEnumerable<Claim> Claims { get; internal set; }
}