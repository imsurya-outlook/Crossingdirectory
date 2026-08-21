namespace YellowPages.Web.Models;

public class AdPhoto
{
    public int Id { get; set; }
    public int AdvertisementId { get; set; }
    public Advertisement? Advertisement { get; set; }

    /// <summary>Public URL (blob) or app-relative path (local disk).</summary>
    public string Url { get; set; } = string.Empty;
    public string? StorageKey { get; set; }
    public string? Caption { get; set; }
    public string ContentType { get; set; } = "image/jpeg";
    public long SizeBytes { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public DateTime UploadedUtc { get; set; } = DateTime.UtcNow;
}
