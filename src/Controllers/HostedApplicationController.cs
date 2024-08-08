using Microsoft.AspNetCore.Mvc;
using TraefikForwardAuth.Models;

namespace TraefikForwardAuth.Controllers;

public class HostedApplicationController : Controller
{
    private readonly IHostedApplicationService hostedAppService;

    public HostedApplicationController(IHostedApplicationService hostedAppService)
    {
        this.hostedAppService = hostedAppService;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new HostedApplicationViewModel
        {
            HostedApps = await hostedAppService.GetApplications(),
        };

        return View(vm);
    }
}