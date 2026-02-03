using Microsoft.EntityFrameworkCore;
using FGC.Users.Domain.Entities;
using System.Text.Json;

namespace FGC.Users.Infrastructure;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<AuditEvent> AuditEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<AuditEvent>(b =>
        {
            b.HasKey(x => x.Id);
        });

        base.OnModelCreating(modelBuilder);
    }
}
