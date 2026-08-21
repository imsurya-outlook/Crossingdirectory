using Microsoft.AspNetCore.Identity;

namespace YellowPages.Web.Models;

/// <summary>
/// Registered end user. Email + mobile number are both mandatory at registration
/// (enforced in RegisterViewModel and by the unique indexes configured in AppDbContext).
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? Locality { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginUtc { get; set; }

    public ICollection<Advertisement> Advertisements { get; set; } = new List<Advertisement>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
