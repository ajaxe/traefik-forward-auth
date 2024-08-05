namespace TraefikForwardAuth.Configuration;

public class AppOptions
{
    public const string SectionName = nameof(AppOptions);
    public string Username { get; set; }
    public string Password { get; set; }
}