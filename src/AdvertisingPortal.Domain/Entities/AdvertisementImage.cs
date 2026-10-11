using AdvertisingPortal.Domain.Common;

namespace AdvertisingPortal.Domain.Entities;

public class AdvertisementImage : BaseEntity
{
    public int AdvertisementId { get; set; }
    public Advertisement Advertisement { get; set; } = default!;

    public string FileName { get; set; } = default!;
    public string FilePath { get; set; } = default!;
    public string ContentType { get; set; } = default!;

    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
    public long FileSizeBytes { get; set; }
}
