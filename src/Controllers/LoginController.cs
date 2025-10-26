using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using TraefikForwardAuth.Auth;
using TraefikForwardAuth.Models;

namespace TraefikForwardAuth.Controllers;

public class LoginController : Controller
{
    private const string LoginErrorKey = "loginError";
    private const string ReturnUrlKey = "returnUrl";
    private const string PostLoginKey = "postLogin";
    private static string ServiceTokenHeaderKey => CustomCookieAuthenticationEvents.ServiceTokenHeaderKey;
    private readonly ILogger<LoginController> logger;

    public LoginController(ILogger<LoginController> logger)
    {
        this.logger = logger;
    }
    public IActionResult Index(string? returnUrl = null)
    {
        if (this.Request.Headers.TryGetValue(ServiceTokenHeaderKey, out var tokenHeader)
            && tokenHeader.Any())
        {
            var token = tokenHeader.First();
            logger.LogInformation("Login Index: {@ServiceTokenHeader} value: {@token}", ServiceTokenHeaderKey, token);
        }
        else
        {
            logger.LogInformation("Login Index: {@ServiceTokenHeader} is not present. Request Header: {headers}",
                ServiceTokenHeaderKey, this.Request.Headers);
        }
        var vm = TempData.Get<LoginViewModel>(LoginErrorKey) ?? new LoginViewModel();
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            TempData.Put(ReturnUrlKey, returnUrl);
        }
        return View(vm);
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        if (this.User.Identity!.IsAuthenticated)
            await HttpContext.SignOutAsync(User.Identity.AuthenticationType);

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Check(string? token,
        [FromServices] IAuthService authService)
    {
        if (!this.User.Identity!.IsAuthenticated)
        {
            logger.LogInformation("User is not authenticated");
            return Forbid();
        }

        if (Request.Headers.TryGetValue(ServiceTokenHeaderKey, out var tokenHeader)
            && tokenHeader.Any())
        {
            token = tokenHeader.First();
            logger.LogInformation("{@ServiceTokenHeader} value: {@token}", ServiceTokenHeaderKey, token);
        }
        else
        {
            logger.LogInformation("{@ServiceTokenHeader} is not present. Request Header: {headers}",
                ServiceTokenHeaderKey, this.Request.Headers);
        }


        string serviceUrl = await authService.AuthCheck(new AuthCheckData
        {
            ServiceToken = token ?? string.Empty,
            Claims = User.Claims,
        });

        if (string.IsNullOrWhiteSpace(serviceUrl))
        {
            logger.LogInformation("Invalid {@ServiceUrl} for {@Token}, returning forbidden", serviceUrl, token);
            return Forbid();
        }

        var isPostLogin = TempData.Get<string>(PostLoginKey) == "true";

        if (isPostLogin)
        {
            logger.LogInformation("First time request redirect: {redirect}, setting 'PostLoginKey false", token);
            TempData.Put(PostLoginKey, "false");
            return Redirect(serviceUrl);
        }

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginSubmit(LoginBindingModel model,
        [FromServices] IAuthService authService)
    {
        if (!TryValidateModel(model))
        {
            TempData.Put(LoginErrorKey, model.Error("Username and password are required"));
            return Redirect("Index");
        }
        var returnUrl = TempData.Get<string>(ReturnUrlKey) ?? "/";

        var result = await authService.Authenticate(new AuthenticateData
        {
            Username = model.Username,
            Password = model.Password,
            RequestingDomain = this.Request.Host.Host,
        });

        if (result.Success)
        {
            await HttpContext.SignInAsync(
                result.Principal.Identity!.AuthenticationType,
                result.Principal,
                result.AuthProperties);

            logger.LogInformation("Setting 'PostLoginKey' to 1. User authenticated: {@Username} {@PostLoginRedirect} {@AuthenticationType}",
                model.Username, returnUrl, result.Principal.Identity!.AuthenticationType);
            TempData.Put(PostLoginKey, "true");

            return LocalRedirect(returnUrl);
        }
        else TempData.Put(LoginErrorKey, model.Error("Invalid username or password"));

        return Redirect("Index");
    }

    [HttpGet]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Introspect()
    {
        var result = await HttpContext.AuthenticateAsync();

        if (!result.Succeeded)
        {
            logger.LogInformation("Introspect failed, user not authenticated");
            return Forbid();
        }

        return Json(new IntrospectResponse
        {
            Active = true,
            Username = result.Principal.FindFirstValue(ClaimTypes.Name),
            IssuedUtc = result.Ticket.Properties.IssuedUtc.GetValueOrDefault()
        });
    }
}