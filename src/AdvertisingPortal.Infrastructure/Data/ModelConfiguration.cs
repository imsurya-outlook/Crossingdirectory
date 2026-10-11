using AdvertisingPortal.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdvertisingPortal.Infrastructure.Data;

/// <summary>
/// Mappings that matter across both providers, so the SQLite dev schema and the PostgreSQL
/// production schema stay in step. Applied once from ApplicationDbContext.
/// </summary>
public static class ModelConfiguration
{
    public static void ApplyIdentityDefaults(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FullName).HasMaxLength(100);
            e.Property(u => u.Id).HasMaxLength(450);
            e.Property(u => u.UserName).HasMaxLength(256);
            e.Property(u => u.NormalizedUserName).HasMaxLength(256);
            e.Property(u => u.Email).HasMaxLength(256);
            e.Property(u => u.NormalizedEmail).HasMaxLength(256);
            e.Property(u => u.ConcurrencyStamp).HasMaxLength(256);
            e.Property(u => u.SecurityStamp).HasMaxLength(256);
            e.Property(u => u.PasswordHash).HasMaxLength(512);
            e.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex");
            e.HasIndex(u => u.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique();
        });

        builder.Entity<IdentityRole>(e =>
        {
            e.Property(r => r.Id).HasMaxLength(450);
            e.Property(r => r.Name).HasMaxLength(256);
            e.Property(r => r.NormalizedName).HasMaxLength(256);
            e.Property(r => r.ConcurrencyStamp).HasMaxLength(256);
            e.HasIndex(r => r.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique();
        });

        builder.Entity<IdentityUserRole<string>>(e =>
        {
            e.Property(r => r.UserId).HasMaxLength(450);
            e.Property(r => r.RoleId).HasMaxLength(450);
        });

        builder.Entity<IdentityUserClaim<string>>(e => e.Property(c => c.UserId).HasMaxLength(450));

        builder.Entity<IdentityUserLogin<string>>(e =>
        {
            e.Property(l => l.UserId).HasMaxLength(450);
            e.Property(l => l.LoginProvider).HasMaxLength(128);
            e.Property(l => l.ProviderKey).HasMaxLength(256);
        });

        builder.Entity<IdentityUserToken<string>>(e =>
        {
            e.Property(t => t.UserId).HasMaxLength(450);
            e.Property(t => t.LoginProvider).HasMaxLength(128);
            e.Property(t => t.Name).HasMaxLength(128);
        });

        builder.Entity<IdentityRoleClaim<string>>(e => e.Property(c => c.RoleId).HasMaxLength(450));
    }
}
