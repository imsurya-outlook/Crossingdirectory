using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using YellowPages.Web.Options;

namespace YellowPages.Web.Services;

/// <summary>
/// Production storage for Azure PaaS. App Service instances are stateless and can scale
/// out, so uploaded photos must live in Blob Storage, not on the local filesystem.
/// </summary>
public class AzureBlobPhotoStorage : IPhotoStorage
{
    private readonly BlobContainerClient _container;
    private readonly StorageOptions _options;

    public AzureBlobPhotoStorage(IOptions<StorageOptions> options)
    {
        _options = options.Value;
        var service = new BlobServiceClient(_options.BlobConnectionString);
        _container = service.GetBlobContainerClient(_options.BlobContainer);
        _container.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<StoredPhoto> SaveAsync(Stream content, string originalFileName, string contentType, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(originalFileName);
        var key = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():n}{extension}";
        var blob = _container.GetBlobClient(key);

        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType, CacheControl = "public, max-age=2592000" }
        }, ct);

        var props = await blob.GetPropertiesAsync(cancellationToken: ct);
        var url = string.IsNullOrWhiteSpace(_options.BlobPublicBaseUrl)
            ? blob.Uri.ToString()
            : $"{_options.BlobPublicBaseUrl.TrimEnd('/')}/{key}";

        return new StoredPhoto(url, key, contentType, props.Value.ContentLength);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct = default)
        => await _container.GetBlobClient(storageKey).DeleteIfExistsAsync(cancellationToken: ct);
}
