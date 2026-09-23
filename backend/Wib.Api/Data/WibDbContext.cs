using Microsoft.EntityFrameworkCore;
using Wib.Api.Data.Entities;

namespace Wib.Api.Data;

public class WibDbContext : DbContext
{
    public WibDbContext(DbContextOptions<WibDbContext> options) : base(options)
    {
    }

    public DbSet<Member> Members => Set<Member>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("members");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Auth0UserId).IsRequired().HasMaxLength(255);
            entity.HasIndex(m => m.Auth0UserId).IsUnique();
            entity.Property(m => m.Name).IsRequired().HasMaxLength(255);
            entity.Property(m => m.Email).HasMaxLength(255);
            entity.Property(m => m.Picture).HasMaxLength(1024);
            entity.Property(m => m.WalletBalance).HasDefaultValue(0);
            entity.Property(m => m.EarnedPoints).HasDefaultValue(0);
            entity.Property(m => m.CreatedAt).IsRequired();
            entity.Property(m => m.UpdatedAt);
        });
    }
}

