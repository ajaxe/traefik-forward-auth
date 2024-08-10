namespace TraefikForwardAuth.Abstractions.Services;
public interface IHostedApplicationService
{
    Task<IEnumerable<HostedApplication>> GetApplications();
    Task<HostedApplication?> GetByServicetoken(Guid serviceToken);
}