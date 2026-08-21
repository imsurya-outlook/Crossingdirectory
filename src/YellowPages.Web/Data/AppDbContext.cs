using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YellowPages.Web.Models;

namespace YellowPages.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Advertisement> Advertisements => Set<Advertisement>();
    public DbSet<ServiceOffering> ServiceOfferings => Set<ServiceOffering>();
    public DbSet<AdPhoto> AdPhotos => Set<AdPhoto>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(120).IsRequired();
            e.Property(x => x.Locality).HasMaxLength(120);

            // Mobile number is mandatory at registration and unique across accounts.
            // SQL Server treats NULL as a value in a unique index (so a second NULL
            // would collide); SQLite does not. Filter the index on SQL Server so
            // Identity rows without a phone number remain legal.
            var phoneIndex = e.HasIndex(x => x.PhoneNumber).IsUnique();
            if (Database.IsSqlServer())
                phoneIndex.HasFilter("[PhoneNumber] IS NOT NULL");
        });

        b.Entity<Category>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(80).IsRequired();
            e.Property(x => x.Icon).HasMaxLength(60);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        b.Entity<Advertisement>(e =>
        {
            e.Property(x => x.BusinessName).HasMaxLength(140).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(160).IsRequired();
            e.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            e.Property(x => x.AddressLine).HasMaxLength(250).IsRequired();
            e.Property(x => x.Locality).HasMaxLength(120).IsRequired();
            e.Property(x => x.City).HasMaxLength(120).IsRequired();
            e.Property(x => x.PostalCode).HasMaxLength(12);
            e.Property(x => x.ContactPhone).HasMaxLength(20).IsRequired();
            e.Property(x => x.ContactEmail).HasMaxLength(180);
            e.Property(x => x.WebsiteUrl).HasMaxLength(250);
            e.Property(x => x.WhatsAppNumber).HasMaxLength(20);
            e.Property(x => x.OpeningHours).HasMaxLength(180);
            e.Property(x => x.ModerationNote).HasMaxLength(500);

            e.HasIndex(x => new { x.Status, x.CategoryId });
            e.HasIndex(x => x.Locality);
            e.HasIndex(x => x.Slug);

            e.HasOne(x => x.Category)
                .WithMany(c => c.Advertisements)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Owner)
                .WithMany(u => u.Advertisements)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.ServiceOfferings)
                .WithOne(s => s.Advertisement!)
                .HasForeignKey(s => s.AdvertisementId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Photos)
                .WithOne(p => p.Advertisement!)
                .HasForeignKey(p => p.AdvertisementId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ServiceOffering>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Details).HasMaxLength(400);
            e.Property(x => x.PriceUnit).HasMaxLength(40);
            // SQLite has no decimal type; store money as TEXT-safe decimal(18,2).
            e.Property(x => x.PriceFrom).HasColumnType("decimal(18,2)");
        });

        b.Entity<AdPhoto>(e =>
        {
            e.Property(x => x.Url).HasMaxLength(400).IsRequired();
            e.Property(x => x.StorageKey).HasMaxLength(300);
            e.Property(x => x.Caption).HasMaxLength(160);
            e.Property(x => x.ContentType).HasMaxLength(60);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.Property(x => x.TokenHash).HasMaxLength(120).IsRequired();
            e.Property(x => x.ReplacedByTokenHash).HasMaxLength(120);
            e.Property(x => x.CreatedByIp).HasMaxLength(64);
            e.HasIndex(x => x.TokenHash);
            e.HasOne(x => x.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Ignore(x => x.IsActive);
        });
    }
}
