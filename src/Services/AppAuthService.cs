using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using TraefikForwardAuth.Helpers;

namespace TraefikForwardAuth.Services;

public class AppAuthService : IAuthService
{
    private readonly IHostedApplicationService appService;
    private readonly AppDbContext dbContext;
    private readonly ILogger<AppAuthService> logger;

    public AppAuthService(IHostedApplicationService appService,
        AppDbContext dbContext, ILogger<AppAuthService> logger)
    {
        this.appService = appService;
        this.dbContext = dbContext;
        this.logger = logger;
    }

    public async Task<AuthenticationResult> Authenticate(string username, string password)
    {
        // add password hashing later
        var existing = await dbContext.AppUsers.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == username && u.Password == password);

        if (existing is not null)
        {
            return new AuthenticationResult
            {
                Success = true,
                UserId = existing.Id.ToString(),
                Principal = BuildPrincipal(existing),
                AuthProperties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    IsPersistent = true,
                    IssuedUtc = DateTimeOffset.UtcNow,
                }
            };
        }
        else
        {
            return new AuthenticationResult
            {
                Success = false,
            };
        }
    }

    public async Task<string> AuthCheck(AuthCheckData authCheckData)
    {
        var existingService = await appService.GetByServiceToken(authCheckData.ServiceToken);
        if (existingService is null || !existingService.Active)
        {
            logger.LogInformation("Invalid service. {@data}",
            new
            {
                ServiceActive = existingService?.Active,
                authCheckData.ServiceToken,
                ClaimCount = authCheckData.Claims.Count(),
                Claims = authCheckData.Claims.Select(c => new { c.Type, c.Value })
            });
            return string.Empty;
        }

        logger.LogInformation("Found service. {@data}", existingService);

        var allowedAppIds = GetClaimAppIds(authCheckData.Claims);

        var serviceId = existingService.Id.ToString();
        if (allowedAppIds.Any(a => a == serviceId))
        {
            return existingService.ServiceUrl;
        }

        return string.Empty;
    }

    private ClaimsPrincipal BuildPrincipal(AppUser existing)
    {
        var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, existing.UserName),
                new Claim(ClaimTypes.PrimarySid, existing.Id.ToString()),
                new Claim(CustomClaimTypes.AppIds, existing.Applications
                                                    .Select(a => a.HostAppId.ToString())
                                                    .ToJson()),
            };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(claimsIdentity);
    }

    private List<string> GetClaimAppIds(IEnumerable<Claim> claims)
    {
        var json = claims.FirstOrDefault(
            c => c.Type == CustomClaimTypes.AppIds)?
            .Value;

        if (!string.IsNullOrWhiteSpace(json))
        {
            return JsonSerializer.Deserialize<List<string>>(json)!;
        }
        return new List<string>();
    }
}