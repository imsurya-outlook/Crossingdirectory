namespace AdvertisingPortal.Application.Common;

public static class UploadLimits
{
    public const int MaxImagesPerAdvertisement = 10;
    public const long MaxImageSizeBytes = 5 * 1024 * 1024;

    public static readonly string[] AllowedContentTypes =
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
}
