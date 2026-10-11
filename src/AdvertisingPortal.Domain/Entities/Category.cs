using AdvertisingPortal.Domain.Common;

namespace AdvertisingPortal.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ICollection<Advertisement> Advertisements { get; set; } = new List<Advertisement>();
}
