using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
            TempData.Put<string>(ReturnUrlKey, returnUrl);
        }
        return View(vm);
    }

    [Authorize]
    public IActionResult Check(string redirect)
    {
        if (!this.User.Identity!.IsAuthenticated)
        {
            return Forbid();
        }

        var isPostLogin = TempData.Get<string>(PostLoginKey) == "true";
        logger.LogInformation("Reading TempData - PostLoginKey. {isPostLogin}", isPostLogin);
        if (isPostLogin)
        {
            logger.LogInformation("First time request redirect: {redirect}", redirect);
            TempData.Put(PostLoginKey, "false");
            return Redirect(redirect!);
        }

        logger.LogInformation("Request redirect: {redirect}", redirect);
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginSubmit(LoginBindingModel model)
    {
        if (!TryValidateModel(model))
        {
            TempData.Put(LoginErrorKey, model.Error("Username and password are required"));
            return Redirect("Index");
        }
        var returnUrl = TempData.Get<string>(ReturnUrlKey);
        if (!string.IsNullOrWhiteSpace(returnUrl) && model.Username == "admin")
        {
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = true,
                IssuedUtc = DateTimeOffset.UtcNow,
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.Username),
                new Claim(ClaimTypes.Role, "Administrator"),
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            logger.LogInformation("Setting 'PostLoginKey' to 1");
            TempData.Put(PostLoginKey, "true");

            return LocalRedirect(returnUrl);
        }
        else TempData.Put(LoginErrorKey, model.Error("Invalid username or password"));

        return Redirect("Index");
    }
}