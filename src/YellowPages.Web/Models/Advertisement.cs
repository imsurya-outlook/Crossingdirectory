namespace YellowPages.Web.Models;

public class Advertisement
{
    public int Id { get; set; }

    public string BusinessName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser? Owner { get; set; }

    // Local scope: locality + city are what the directory searches on.
    public string AddressLine { get; set; } = string.Empty;
    public string Locality { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PostalCode { get; set; }

    public string ContactPhone { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string? OpeningHours { get; set; }

    public AdStatus Status { get; set; } = AdStatus.Pending;
    public string? ModerationNote { get; set; }
    public bool IsFeatured { get; set; }

    public int ViewCount { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
    public DateTime? ReviewedUtc { get; set; }

    public ICollection<ServiceOffering> ServiceOfferings { get; set; } = new List<ServiceOffering>();
    public ICollection<AdPhoto> Photos { get; set; } = new List<AdPhoto>();
}
