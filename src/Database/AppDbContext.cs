using Microsoft.EntityFrameworkCore;
using MongoDB.Bson.Serialization.Conventions;
using TraefikForwardAuth.Database.Models;

namespace TraefikForwardAuth.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    { }
    public DbSet<HostedApplication> HostedApplications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var conventionPack = new ConventionPack
        {
            new CamelCaseElementNameConvention()
        };
        ConventionRegistry.Register("Camel Case", conventionPack, t => true);

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<HostedApplication>();
    }
}