using Microsoft.Extensions.Options;
using YellowPages.Web.Options;

namespace YellowPages.Web.Services;

/// <summary>Development-time storage under wwwroot. Not suitable for scaled-out App Service.</summary>
public class LocalDiskPhotoStorage : IPhotoStorage
{
    private readonly IWebHostEnvironment _env;
    private readonly StorageOptions _options;

    public LocalDiskPhotoStorage(IWebHostEnvironment env, IOptions<StorageOptions> options)
    {
        _env = env;
        _options = options.Value;
    }

    public async Task<StoredPhoto> SaveAsync(Stream content, string originalFileName, string contentType, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(originalFileName);
        var key = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():n}{extension}";
        var relative = Path.Combine(_options.LocalFolder.Replace('/', Path.DirectorySeparatorChar),
                                    key.Replace('/', Path.DirectorySeparatorChar));
        var absolute = Path.Combine(_env.WebRootPath, relative);

        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        await using var file = File.Create(absolute);
        await content.CopyToAsync(file, ct);

        var url = "/" + Path.Combine(_options.LocalFolder, key).Replace(Path.DirectorySeparatorChar, '/');
        return new StoredPhoto(url, key, contentType, file.Length);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var relative = Path.Combine(_options.LocalFolder.Replace('/', Path.DirectorySeparatorChar),
                                    storageKey.Replace('/', Path.DirectorySeparatorChar));
        var absolute = Path.Combine(_env.WebRootPath, relative);
        if (File.Exists(absolute)) File.Delete(absolute);
        return Task.CompletedTask;
    }
}
