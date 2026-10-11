using Microsoft.AspNetCore.Identity;

namespace AdvertisingPortal.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Advertisement> Advertisements { get; set; } = new List<Advertisement>();
}
