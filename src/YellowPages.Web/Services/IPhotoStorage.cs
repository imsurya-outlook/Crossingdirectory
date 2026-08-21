namespace YellowPages.Web.Services;

public record StoredPhoto(string Url, string StorageKey, string ContentType, long SizeBytes);

public interface IPhotoStorage
{
    Task<StoredPhoto> SaveAsync(Stream content, string originalFileName, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}
