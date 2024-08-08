
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
}