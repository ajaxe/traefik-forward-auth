using Microsoft.Extensions.DependencyInjection;
using TraefikForwardAuth.Database;
using Xunit.Abstractions;

namespace TraefikForwardAuth.Tests.Integration;

public class DbContextIntegrationTests : AppTestBase
{
    public DbContextIntegrationTests(ITestOutputHelper output) : base(output) { }
    [Fact]
    public void DbContext_Read_HostApps_OK()
    {
        var dbctx = ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var h in dbctx.HostedApplications)
            OutputHelper.WriteLine("Url: {0}, Token: {1}", h.ServiceUrl, h.ServiceToken);
    }
}