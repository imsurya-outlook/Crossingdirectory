namespace YellowPages.Web.Options;

public class StorageOptions
{
    /// <summary>LocalDisk (development) or AzureBlob (Azure PaaS).</summary>
    public string Provider { get; set; } = "LocalDisk";
    public string LocalFolder { get; set; } = "uploads/ads";
    public string BlobConnectionString { get; set; } = string.Empty;
    public string BlobContainer { get; set; } = "ad-photos";
    public string BlobPublicBaseUrl { get; set; } = string.Empty;
}
