namespace YellowPages.Web.Services;

public static class PhotoValidator
{
    public const int MaxPhotosPerAd = 8;
    public const long MaxBytesPerPhoto = 4 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public static bool IsAllowed(IFormFile file, out string error)
    {
        error = string.Empty;

        if (file.Length == 0)
        {
            error = $"{file.FileName} is empty.";
            return false;
        }

        if (file.Length > MaxBytesPerPhoto)
        {
            error = $"{file.FileName} is larger than 4 MB.";
            return false;
        }

        if (!AllowedContentTypes.Contains(file.ContentType?.ToLowerInvariant()))
        {
            error = $"{file.FileName} is not a JPEG, PNG, or WebP image.";
            return false;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            error = $"{file.FileName} has an unsupported file extension.";
            return false;
        }

        return true;
    }
}
