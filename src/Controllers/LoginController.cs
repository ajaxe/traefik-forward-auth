using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using MongoDB.Bson;
using TraefikForwardAuth.Helpers;
using TraefikForwardAuth.Models;

namespace TraefikForwardAuth.Controllers;

public class LoginController : Controller
{
    private const string LoginErrorKey = "loginError";
    private const string ReturnUrlKey = "returnUrl";
    private const string PostLoginKey = "postLogin";
    private readonly ILogger<LoginController> logger;

    public LoginController(ILogger<LoginController> logger)
    {
        this.logger = logger;
    }
    public IActionResult Index(string? returnUrl = null)
    {
        var vm = TempData.Get<LoginViewModel>(LoginErrorKey) ?? new LoginViewModel();
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            TempData.Put(ReturnUrlKey, returnUrl);
        }
        return View(vm);
    }

    [Authorize]
    public async Task<IActionResult> Check(Guid token,
        [FromServices] IAuthService authService)
    {
        if (!this.User.Identity!.IsAuthenticated)
        {
            logger.LogInformation("User is not authenticated");
            return Forbid();
        }

        string serviceUrl = await authService.AuthCheck(new AuthCheckData
        {
            ServiceToken = token,
            Claims = User.Claims,
        });

        if (string.IsNullOrWhiteSpace(serviceUrl))
        {
            //logger.LogInformation("Invalid service, token: {token} active: {active}",
            //token, existing?.Active);
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
        var returnUrl = TempData.Get<string>(ReturnUrlKey);

        var result = await authService.Authenticate(model.Username, model.Password);

        if (!string.IsNullOrWhiteSpace(returnUrl) && result.Success)
        {
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                result.Principal,
                result.AuthProperties);

            logger.LogInformation("Setting 'PostLoginKey' to 1. User authenticated: {user}",
                model.Username);
            TempData.Put(PostLoginKey, "true");

            return LocalRedirect(returnUrl);
        }
        else TempData.Put(LoginErrorKey, model.Error("Invalid username or password"));

        return Redirect("Index");
    }
}