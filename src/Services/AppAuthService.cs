using System.Runtime.Intrinsics.Arm;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using TraefikForwardAuth.Helpers;
using ZstdSharp.Unsafe;

namespace TraefikForwardAuth.Services;

public class AppAuthService : IAuthService
{
    private const int SaltByteLength = 10;
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
        var existing = await dbContext.AppUsers
            .FirstOrDefaultAsync(u => u.UserName == username);

        var passwordMatch = false;

        if (existing?.Password == password)
        {
            logger.LogInformation("Password match, but need hashing.");
            existing.Password = HashPassword(existing.Password);
            await dbContext.SaveChangesAsync();

            passwordMatch = true;
        }
        else
        {
            passwordMatch = ValidatePassword(existing, password);
        }

        if (existing is not null && passwordMatch)
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

    private bool ValidatePassword(AppUser? existing, string password)
    {
        _ = existing ?? throw new ArgumentNullException(nameof(existing));

        var allBtyes = Base64UrlTextEncoder.Decode(existing.Password);
        var salt = allBtyes.Take(SaltByteLength).ToArray();
        var cipher = allBtyes.Skip(SaltByteLength).ToArray();

        var rehash = GeneratePasswordHash(password, salt);

        return cipher.SequenceEqual(rehash);
    }

    private string HashPassword(string password)
    {
        var salt = new byte[SaltByteLength];
        RandomNumberGenerator.Fill(salt);
        byte[] hashBytes;

        hashBytes = GeneratePasswordHash(password, salt);

        return Base64UrlTextEncoder.Encode(salt.Concat(hashBytes).ToArray());
    }

    private static byte[] GeneratePasswordHash(string password, byte[] salt)
    {
        byte[] hashBytes;
        using (var hash = SHA256.Create())
        {
            // hash salt + password bytes
            hashBytes = hash.ComputeHash(salt.Concat(Encoding.UTF8.GetBytes(password)).ToArray());
        }

        return hashBytes;
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
        else
        {
            logger.LogInformation("No application id match, {@AllowedAppId} & {@ExistngAppId}",
                allowedAppIds, serviceId);
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