namespace AdvertisingPortal.Application.Interfaces;

/// <summary>
/// Pictures of the WhatsApp shops directory (public.shops.picture), shown for advertisements linked to a shop
/// ("ShopId") that have no uploaded images of their own.
/// </summary>
public interface IShopImageService
{
    /// <summary>Picture URLs of the given shops that have a picture, by shop id. Shops without one are left out.</summary>
    Task<IReadOnlyDictionary<int, string>> GetPictureUrlsAsync(IEnumerable<int> shopIds, CancellationToken ct = default);

    /// <summary>The stored picture, or null when the shop has none.</summary>
    Task<ShopPicture?> GetPictureAsync(int shopId, CancellationToken ct = default);
}

public sealed record ShopPicture(byte[] Data, string ContentType);
