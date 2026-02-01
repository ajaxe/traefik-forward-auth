using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;
using TraefikForwardAuth.Helpers;
using ZstdSharp.Unsafe;

namespace TraefikForwardAuth.Services;

public class AppTicketStore(IDistributedCache cache,
        IDataSerializer<AuthenticationTicket> serializer,
        ILogger<AppTicketStore> logger) : ITicketStore
{
    private const string KeyPrefix = "ckticket:";
    private const int ExpirationIntervalDays = 1;
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
        using var _ = ActivitySources.AppActivitySource.StartActivity("AppTicketStore.RetrieveAsync");
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
        using var _ = ActivitySources.AppActivitySource.StartActivity("AppTicketStore.StoreInternalAsync");
        var bytes = serializer.Serialize(ticket);
        return cache.SetAsync(key, bytes, GetCacheEntryOptions());
    }
    private static string GenerateKey() => Guid.NewGuid().ToString();
    private static string GetKey(string key) => $"{KeyPrefix}{key}";

    private static DistributedCacheEntryOptions GetCacheEntryOptions()
        => new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(ExpirationIntervalDays),
        };
}
