using Microsoft.EntityFrameworkCore;
using Wib.Api.Data.Entities;

namespace Wib.Api.Data;

public class WibDbContext : DbContext
{
    public WibDbContext(DbContextOptions<WibDbContext> options) : base(options)
    {
    }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<Chore> Chores => Set<Chore>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ChoreTag> ChoreTags => Set<ChoreTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Member>(entity =>
        {
            entity.ToTable("members");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.ExternalSubjectId).IsRequired().HasMaxLength(255);
            entity.HasIndex(m => m.ExternalSubjectId).IsUnique();
            entity.Property(m => m.Name).IsRequired().HasMaxLength(255);
            entity.Property(m => m.WalletBalance).HasDefaultValue(0);
            entity.Property(m => m.CreatedAt).IsRequired();
            entity.Property(m => m.UpdatedAt);
        });

        modelBuilder.Entity<Chore>(entity =>
        {
            entity.ToTable("chores");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Title).IsRequired().HasMaxLength(255);
            entity.Property(c => c.Description).HasMaxLength(2000);
            entity.Property(c => c.Points);
            entity.Property(c => c.CadenceDays);
            entity.Property(c => c.LastCompletedAt);
            entity.Property(c => c.IsArchived);
            entity.Property(c => c.CreatedAt).IsRequired();
            entity.Property(c => c.UpdatedAt);
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.ToTable("tags");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(t => t.Name).IsUnique();
            entity.Property(t => t.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<ChoreTag>(entity =>
        {
            entity.ToTable("chore_tags");
            entity.HasKey(ct => new { ct.ChoreId, ct.TagId });
            entity.HasOne(ct => ct.Chore)
                .WithMany(c => c.ChoreTags)
                .HasForeignKey(ct => ct.ChoreId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ct => ct.Tag)
                .WithMany(t => t.ChoreTags)
                .HasForeignKey(ct => ct.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
