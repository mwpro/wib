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
    public DbSet<ChoreCompletion> ChoreCompletions => Set<ChoreCompletion>();
    public DbSet<RewardItem> RewardItems => Set<RewardItem>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();

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
            entity.HasMany(m => m.Vouchers)
                .WithOne(v => v.OwnedByMember)
                .HasForeignKey(v => v.OwnedByMemberId)
                .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<ChoreCompletion>(entity =>
        {
            entity.ToTable("chore_completions");
            entity.HasKey(cc => cc.Id);
            entity.Property(cc => cc.CompletedAt).IsRequired();
            entity.Property(cc => cc.PointsAwarded).IsRequired();
            entity.HasOne(cc => cc.Chore)
                .WithMany(c => c.Completions)
                .HasForeignKey(cc => cc.ChoreId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(cc => cc.CompletedByMember)
                .WithMany()
                .HasForeignKey(cc => cc.CompletedByMemberId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RewardItem>(entity =>
        {
            entity.ToTable("reward_items");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Title).IsRequired().HasMaxLength(255);
            entity.Property(r => r.Description).HasMaxLength(2000);
            entity.Property(r => r.PointCost).IsRequired();
            entity.Property(r => r.Quantity);
            entity.Property(r => r.IsActive).HasDefaultValue(true);
            entity.Property(r => r.CreatedAt).IsRequired();
            entity.Property(r => r.UpdatedAt);
            entity.HasOne(r => r.CreatedByMember)
                .WithMany()
                .HasForeignKey(r => r.CreatedByMemberId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.ToTable("vouchers");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.TitleSnapshot).IsRequired().HasMaxLength(255);
            entity.Property(v => v.PointCostSnapshot).IsRequired();
            entity.Property(v => v.PurchasedAt).IsRequired();
            entity.Property(v => v.RedeemedAt);
            entity.HasOne(v => v.RewardItem)
                .WithMany()
                .HasForeignKey(v => v.RewardItemId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(v => v.OwnedByMember)
                .WithMany(m => m.Vouchers)
                .HasForeignKey(v => v.OwnedByMemberId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();

        configurationBuilder.Properties<DateTime?>()
            .HaveConversion<NullableUtcDateTimeConverter>();
    }
}

public class UtcDateTimeConverter : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

public class NullableUtcDateTimeConverter : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter() : base(
        v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)) : v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
    {
    }
}
