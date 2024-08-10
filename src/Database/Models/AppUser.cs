using MongoDB.Bson;
using MongoDB.EntityFrameworkCore;

namespace TraefikForwardAuth.Database.Models;

[Collection("app_users")]
public class AppUser
{
    public AppUser()
    {
        Applications = new List<AppUserApplication>();
    }
    public ObjectId Id { get; set; }
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public bool Active { get; set; }
    public List<AppUserApplication> Applications { get; set; }
}

public class AppUserApplication
{
    public ObjectId HostAppId { get; set; }
}