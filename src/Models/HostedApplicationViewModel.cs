using TraefikForwardAuth.Database.Models;

namespace TraefikForwardAuth.Models;

public class HostedApplicationViewModel
{
    public HostedApplicationViewModel()
    {
        HostedApps = new List<HostedApplication>();
    }
    public IEnumerable<HostedApplication> HostedApps { get; set; }
}