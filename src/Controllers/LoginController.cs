using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using TraefikForwardAuth.Models;

namespace TraefikForwardAuth.Controllers;

public class LoginController : Controller
{
    private const string LoginErrorKey = "loginError";
    private const string ReturnUrlKey = "returnUrl";
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
    public IActionResult Check() => Ok();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult LoginSubmit(LoginBindingModel model)
    {
        if (!TryValidateModel(model))
        {
            TempData.Put(LoginErrorKey, model.Error("Username and password are required"));
            return Redirect("Index");
        }
        var returnUrl = TempData.Get<string>(ReturnUrlKey);
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            TempData.Put(LoginErrorKey, model.Error("Has return URL"));
        }
        else TempData.Put(LoginErrorKey, model.Error("Invalid username or password"));

        return Redirect("Index");
    }
}