using AdvertisingPortal.Application.DTOs;

namespace AdvertisingPortal.Application.Interfaces;

public interface IFileStorageService
{
    Task<FileUploadResult> SaveAsync(UploadedFileDto file, string folderPath, CancellationToken ct = default);
    Task DeleteAsync(string filePath, CancellationToken ct = default);
    string GetPublicUrl(string filePath);
}
