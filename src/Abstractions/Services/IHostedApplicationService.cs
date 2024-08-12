
namespace TraefikForwardAuth.Abstractions.Services;
public interface IHostedApplicationService
{
    Task<IEnumerable<HostedApplication>> GetApplications();
    Task<IEnumerable<HostedApplication>> GetApplications(List<string> appIds);
    Task<HostedApplication?> GetByServiceToken(string serviceToken);
}