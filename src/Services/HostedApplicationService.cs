
using Microsoft.EntityFrameworkCore;

namespace TraefikForwardAuth.Services;

public class HostedApplicationService : IHostedApplicationService
{
    private readonly AppDbContext dbContext;

    public HostedApplicationService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    public async Task<IEnumerable<HostedApplication>> GetApplications()
    {
        return await dbContext.HostedApplications.AsNoTracking().ToListAsync();
    }

    public async Task<HostedApplication?> GetByServicetoken(Guid serviceToken)
    {
        if (serviceToken == default)
        {
            return null;
        }

        return await dbContext.HostedApplications.AsNoTracking()
            .FirstOrDefaultAsync(s => s.ServiceToken == serviceToken);
    }
}