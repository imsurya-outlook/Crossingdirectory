using AdvertisingPortal.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AdvertisingPortal.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Advertisement> Advertisements => Set<Advertisement>();
    public DbSet<AdvertisementImage> AdvertisementImages => Set<AdvertisementImage>();
    public DbSet<AdvertisementAnalytics> AdvertisementAnalytics => Set<AdvertisementAnalytics>();
    public DbSet<AdvertisementRating> AdvertisementRatings => Set<AdvertisementRating>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Locality> Localities => Set<Locality>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Identity column lengths + index names, applied once for every provider.
        ModelConfiguration.ApplyIdentityDefaults(builder);

        builder.Entity<Category>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<Locality>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<Advertisement>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(150).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(180).IsRequired();
            e.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            e.Property(x => x.ContactName).HasMaxLength(100).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150);
            e.Property(x => x.WebsiteUrl).HasMaxLength(250);
            e.Property(x => x.AddressLine).HasMaxLength(300).IsRequired();
            e.Property(x => x.ModerationRemarks).HasMaxLength(500);

            e.HasIndex(x => x.Slug).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.IsFeatured);
            e.HasIndex(x => x.IsVerified);
            e.HasIndex(x => x.CreatedOnUtc);
            e.HasIndex(x => new { x.Status, x.IsActive, x.CategoryId, x.LocalityId });

            e.HasOne(x => x.Category)
                .WithMany(c => c.Advertisements)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Locality)
                .WithMany(l => l.Advertisements)
                .HasForeignKey(x => x.LocalityId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CreatedByUser)
                .WithMany(u => u.Advertisements)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Analytics)
                .WithOne(a => a.Advertisement)
                .HasForeignKey<AdvertisementAnalytics>(a => a.AdvertisementId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<AdvertisementImage>(e =>
        {
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.AdvertisementId);

            e.HasOne(x => x.Advertisement)
                .WithMany(a => a.Images)
                .HasForeignKey(x => x.AdvertisementId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<AdvertisementRating>(e =>
        {
            e.Property(x => x.Comment).HasMaxLength(600);
            e.HasIndex(x => new { x.AdvertisementId, x.CreatedByUserId }).IsUnique();

            e.HasOne(x => x.Advertisement)
                .WithMany(a => a.Ratings)
                .HasForeignKey(x => x.AdvertisementId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<AdvertisementAnalytics>(e =>
        {
            e.HasIndex(x => x.AdvertisementId).IsUnique();
            e.HasQueryFilter(x => !x.IsDeleted);
        });
    }
}
