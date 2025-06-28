using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;

namespace TraefikForwardAuth.Services;

public class AppTicketStore : ITicketStore
{
    private const string KeyPrefix = "ckticket:";
    private const int ExpirationIntervalDays = 1;
    private readonly IDistributedCache cache;
    private readonly IDataSerializer<AuthenticationTicket> serializer;
    private readonly ILogger<AppTicketStore> logger;

    public AppTicketStore(IDistributedCache cache,
        IDataSerializer<AuthenticationTicket> serializer,
        ILogger<AppTicketStore> logger)
    {
        this.cache = cache;
        this.serializer = serializer;
        this.logger = logger;
    }

    public Task RemoveAsync(string key)
    {
        return cache.RemoveAsync(GetKey(key));
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        return StoreInternalAsync(GetKey(key), ticket);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var bytes = await cache.GetAsync(GetKey(key));
        if (bytes == null || bytes.Length == 0)
        {
            return null;
        }
        return serializer.Deserialize(bytes);
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var k = GenerateKey();
        await StoreInternalAsync(GetKey(k), ticket);
        return k;
    }
    private Task StoreInternalAsync(string key, AuthenticationTicket ticket)
    {
        var bytes = serializer.Serialize(ticket);
        return cache.SetAsync(key, bytes, GetCacheEntryOptions());
    }
    private string GenerateKey() => Guid.NewGuid().ToString();
    private string GetKey(string key) => $"{KeyPrefix}{key}";

    private DistributedCacheEntryOptions GetCacheEntryOptions()
        => new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(ExpirationIntervalDays),
        };
}