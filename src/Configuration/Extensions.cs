namespace TraefikForwardAuth.Configuration;

public static class Extensions
{
    public static string GetAuthCookieDomain(this AppOptions options, string targetDomain)
    {
        if (string.IsNullOrWhiteSpace(options.AuthCookieDomain))
        {
            if (targetDomain.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty; // No cookie domain for localhost
            }
            return targetDomain;
        }

        var domains = options.GetOrderedAuthDomains();

        foreach (var domain in domains)
        {
            if (targetDomain.Contains(domain, StringComparison.OrdinalIgnoreCase))
            {
                return domain;
            }
        }

        return targetDomain;
    }
    public static string GetAuthSchemeName(this AppOptions options, string targetDomain)
    {
        return $"AuthScheme:{GetAuthCookieDomain(options, targetDomain)}";
    }
    public static IEnumerable<string> GetOrderedAuthDomains(this AppOptions options)
    {
        return options.AuthCookieDomain.Split(",", StringSplitOptions.RemoveEmptyEntries)
            .OrderByDescending(d => d.Length)
            .ToArray();
    }
}