namespace YellowPages.Web.Models;

/// <summary>One of the (many) services an advertisement offers.</summary>
public class ServiceOffering
{
    public int Id { get; set; }
    public int AdvertisementId { get; set; }
    public Advertisement? Advertisement { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Details { get; set; }
    public decimal? PriceFrom { get; set; }
    /// <summary>e.g. "per visit", "per hour", "per sq ft".</summary>
    public string? PriceUnit { get; set; }
    public int SortOrder { get; set; }
}
