using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.EntityFrameworkCore;

namespace TraefikForwardAuth.Database.Models;

[Collection("hosted_applications")]
public class HostedApplication
{
    public ObjectId Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = default!;
    [BsonElement("service_token")]
    public string ServiceToken { get; set; } = default!;
    [BsonElement("service_url")]
    public string ServiceUrl { get; set; } = default!;
    [BsonElement("active")]
    public bool Active { get; set; }
}