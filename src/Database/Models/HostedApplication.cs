using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.EntityFrameworkCore;

namespace TraefikForwardAuth.Database.Models;

[Collection("hosted_applications")]
public class HostedApplication
{
    public ObjectId Id { get; set; }

    public string Name { get; set; } = default!;

    public string ServiceToken { get; set; } = default!;
    public string ServiceUrl { get; set; } = default!;
    public bool Active { get; set; }
}