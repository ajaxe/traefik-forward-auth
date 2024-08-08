namespace TraefikForwardAuth.Configuration;

public class AppOptions
{
    public const string SectionName = nameof(AppOptions);
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string MongoDbConnection { get; set; } = default!;
    public string DatabaseName { get; set; } = default!;
}