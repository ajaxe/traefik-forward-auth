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

        var domains = options.AuthCookieDomain
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .OrderByDescending(d => d.Length)
            .ToArray();

        foreach (var domain in domains)
        {
            if (targetDomain.Contains(domain, StringComparison.OrdinalIgnoreCase))
            {
                return domain;
            }
        }

        return targetDomain;
    }
}