namespace AdvertisingPortal.Application.DTOs;

public class UploadedFileDto
{
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long Length { get; set; }
    public Stream Content { get; set; } = default!;
}
