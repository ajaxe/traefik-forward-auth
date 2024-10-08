namespace TraefikForwardAuth.Models;
public class LoginViewModel
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string ReturnUrl { get; set; } = default!;
    public string ErrorMessage { get; set; } = default!;
}