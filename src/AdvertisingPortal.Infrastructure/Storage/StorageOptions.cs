namespace AdvertisingPortal.Infrastructure.Storage;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "Local";
    public string LocalRootPath { get; set; } = "wwwroot/uploads";
    public string LocalPublicBaseUrl { get; set; } = "/uploads";
    public string ContainerName { get; set; } = "advertisements";
    public string ConnectionString { get; set; } = string.Empty;
    public string? BlobPublicBaseUrl { get; set; }
}
