using AdvertisingPortal.Application.DTOs;
using AdvertisingPortal.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace AdvertisingPortal.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly StorageOptions _options;
    private readonly string _rootPath;

    public LocalFileStorageService(IOptions<StorageOptions> options)
    {
        _options = options.Value;
        _rootPath = Path.IsPathRooted(_options.LocalRootPath)
            ? _options.LocalRootPath
            : Path.Combine(Directory.GetCurrentDirectory(), _options.LocalRootPath);
    }

    public async Task<FileUploadResult> SaveAsync(UploadedFileDto file, string folderPath, CancellationToken ct = default)
    {
        var safeName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(safeName);
        var storedName = Guid.NewGuid().ToString("N") + extension;

        var relativePath = string.Join('/', folderPath.Trim('/'), storedName);
        var absoluteFolder = Path.Combine(_rootPath, folderPath.Trim('/').Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(absoluteFolder);

        var absolutePath = Path.Combine(absoluteFolder, storedName);
        await using var target = File.Create(absolutePath);
        await file.Content.CopyToAsync(target, ct);

        return new FileUploadResult
        {
            FileName = safeName,
            FilePath = relativePath,
            PublicUrl = GetPublicUrl(relativePath)
        };
    }

    public Task DeleteAsync(string filePath, CancellationToken ct = default)
    {
        var absolutePath = Path.Combine(_rootPath, filePath.Trim('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolutePath)) File.Delete(absolutePath);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string filePath)
        => string.Concat(_options.LocalPublicBaseUrl.TrimEnd('/'), "/", filePath.TrimStart('/'));
}
