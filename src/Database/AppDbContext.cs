using Microsoft.EntityFrameworkCore;

namespace TraefikForwardAuth.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    { }
    public DbSet<HostedApplication> HostedApplications { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<HostedApplication>();
        modelBuilder.Entity<AppUser>();
    }
}