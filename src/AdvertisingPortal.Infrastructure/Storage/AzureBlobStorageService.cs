using AdvertisingPortal.Application.DTOs;
using AdvertisingPortal.Application.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace AdvertisingPortal.Infrastructure.Storage;

public class AzureBlobStorageService : IFileStorageService
{
    private readonly StorageOptions _options;
    private readonly BlobContainerClient _container;

    public AzureBlobStorageService(IOptions<StorageOptions> options)
    {
        _options = options.Value;
        _container = new BlobContainerClient(_options.ConnectionString, _options.ContainerName);
    }

    public async Task<FileUploadResult> SaveAsync(UploadedFileDto file, string folderPath, CancellationToken ct = default)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var safeName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(safeName);
        var blobPath = string.Join('/', folderPath.Trim('/'), Guid.NewGuid().ToString("N") + extension);

        var blob = _container.GetBlobClient(blobPath);
        await blob.UploadAsync(file.Content, new BlobHttpHeaders { ContentType = file.ContentType }, cancellationToken: ct);

        return new FileUploadResult
        {
            FileName = safeName,
            FilePath = blobPath,
            PublicUrl = GetPublicUrl(blobPath)
        };
    }

    public async Task DeleteAsync(string filePath, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(filePath.TrimStart('/'));
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
    }

    public string GetPublicUrl(string filePath)
    {
        if (!string.IsNullOrWhiteSpace(_options.BlobPublicBaseUrl))
            return string.Concat(_options.BlobPublicBaseUrl!.TrimEnd('/'), "/", filePath.TrimStart('/'));

        return _container.GetBlobClient(filePath.TrimStart('/')).Uri.ToString();
    }
}
