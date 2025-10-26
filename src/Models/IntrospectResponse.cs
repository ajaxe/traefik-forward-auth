namespace TraefikForwardAuth.Models;

public class IntrospectResponse
{
    public bool Active { get; set; } = true;
    public string? Username { get; set; }
    public DateTimeOffset IssuedUtc { get; set; }
}