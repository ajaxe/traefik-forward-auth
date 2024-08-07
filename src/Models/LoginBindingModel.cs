using System.ComponentModel.DataAnnotations;

namespace TraefikForwardAuth.Models;

public class LoginBindingModel
{
    [Required]
    public string Username { get; set; } = default!;
    [Required]
    public string Password { get; set; } = default!;

    public LoginViewModel Error(string errorMessage)
    {
        return new LoginViewModel
        {
            ErrorMessage = errorMessage,
            Username = Username,
            Password = Password,
        };
    }
}