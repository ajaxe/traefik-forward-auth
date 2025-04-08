namespace TraefikForwardAuth.Configuration;

public class AppOptions
{
    public const string SectionName = nameof(AppOptions);
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string MongoDbConnection { get; set; } = default!;
    public string DatabaseName { get; set; } = default!;
    /// <summary>
    /// Comma delimited domains to use for cookie domain. This is used to set the cookie domain for the auth cookie.
    /// </summary> <summary>
    ///
    /// </summary>
    /// <value></value>
    public string AuthCookieDomain { get; set; } = default!;
}