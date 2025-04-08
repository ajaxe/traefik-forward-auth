using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.EntityFrameworkCore;

namespace TraefikForwardAuth.Database.Models;

[Collection("app_users")]
public class AppUser
{
    public AppUser()
    {
        Applications = new List<AppUserApplication>();
        Roles = new List<string>();
    }
    public ObjectId Id { get; set; }
    [BsonElement("username")]
    public string UserName { get; set; } = default!;
    [BsonElement("password")]
    public string Password { get; set; } = default!;
    [BsonElement("active")]
    public bool Active { get; set; }
    [BsonElement("applications")]
    public List<AppUserApplication> Applications { get; set; }
    [BsonElement("roles")]
    public List<string>? Roles { get; set; }
}

public class AppUserApplication
{
    [BsonElement("host_app_id")]
    public ObjectId HostAppId { get; set; }
}