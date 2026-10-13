using AdvertisingPortal.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AdvertisingPortal.Web.Controllers;

/// <summary>Serves shop pictures stored in public.shops (see <see cref="IShopImageService"/>).</summary>
public class ShopImagesController : Controller
{
    private readonly IShopImageService _shopImages;

    public ShopImagesController(IShopImageService shopImages)
    {
        _shopImages = shopImages;
    }

    // Links carry ?v=<last update>, so a changed picture gets a new URL and each URL can be cached for 30 days.
    [HttpGet("/shop-image/{shopId:int}")]
    [ResponseCache(Duration = 60 * 60 * 24 * 30, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Get(int shopId, CancellationToken ct)
    {
        var picture = await _shopImages.GetPictureAsync(shopId, ct);
        return picture is null ? NotFound() : File(picture.Data, picture.ContentType);
    }
}
