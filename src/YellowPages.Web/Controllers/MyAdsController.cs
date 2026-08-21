using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YellowPages.Web.Data;
using YellowPages.Web.Infrastructure;
using YellowPages.Web.Models;
using YellowPages.Web.Services;
using YellowPages.Web.ViewModels;

namespace YellowPages.Web.Controllers;

/// <summary>Registered members manage their own advertisements here.</summary>
[Authorize]
[Route("my-ads")]
public class MyAdsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPhotoStorage _photos;
    private readonly ILogger<MyAdsController> _logger;

    public MyAdsController(AppDbContext db, IPhotoStorage photos, ILogger<MyAdsController> logger)
    {
        _db = db;
        _photos = photos;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = User.UserId();
        var ads = await _db.Advertisements
            .Where(a => a.OwnerId == userId)
            .Include(a => a.Category)
            .Include(a => a.Photos)
            .Include(a => a.ServiceOfferings)
            .OrderByDescending(a => a.CreatedUtc)
            .ToListAsync(ct);

        return View(ads);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var model = new AdvertisementFormViewModel
        {
            Categories = await ActiveCategoriesAsync(ct),
            Services = new List<ServiceOfferingInput> { new() }
        };
        return View(model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(40 * 1024 * 1024)]
    public async Task<IActionResult> Create(AdvertisementFormViewModel model, CancellationToken ct)
    {
        model.Services = model.Services.Where(s => !string.IsNullOrWhiteSpace(s.Name)).ToList();
        if (model.Services.Count == 0)
            ModelState.AddModelError(nameof(model.Services), "Add at least one service offering.");

        ValidatePhotos(model.NewPhotos, existingCount: 0);

        if (!ModelState.IsValid)
        {
            model.Categories = await ActiveCategoriesAsync(ct);
            if (model.Services.Count == 0) model.Services.Add(new ServiceOfferingInput());
            return View(model);
        }

        var ad = new Advertisement
        {
            OwnerId = User.UserId()!,
            BusinessName = model.BusinessName,
            Slug = SlugBuilder.Create(model.BusinessName),
            Description = model.Description,
            CategoryId = model.CategoryId,
            AddressLine = model.AddressLine,
            Locality = model.Locality,
            City = model.City,
            PostalCode = model.PostalCode,
            ContactPhone = model.ContactPhone,
            ContactEmail = model.ContactEmail,
            WebsiteUrl = model.WebsiteUrl,
            WhatsAppNumber = model.WhatsAppNumber,
            OpeningHours = model.OpeningHours,
            Status = AdStatus.Pending
        };

        for (var i = 0; i < model.Services.Count; i++)
        {
            var s = model.Services[i];
            ad.ServiceOfferings.Add(new ServiceOffering
            {
                Name = s.Name.Trim(),
                Details = s.Details,
                PriceFrom = s.PriceFrom,
                PriceUnit = s.PriceUnit,
                SortOrder = i + 1
            });
        }

        _db.Advertisements.Add(ad);
        await _db.SaveChangesAsync(ct);

        await SavePhotosAsync(ad, model.NewPhotos, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Advertisement {AdId} submitted by {UserId}", ad.Id, ad.OwnerId);
        TempData["Toast"] = "Listing submitted. An administrator will review it shortly.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var ad = await OwnedAdAsync(id, ct);
        if (ad is null) return NotFound();

        var model = new AdvertisementFormViewModel
        {
            Id = ad.Id,
            BusinessName = ad.BusinessName,
            CategoryId = ad.CategoryId,
            Description = ad.Description,
            AddressLine = ad.AddressLine,
            Locality = ad.Locality,
            City = ad.City,
            PostalCode = ad.PostalCode,
            ContactPhone = ad.ContactPhone,
            ContactEmail = ad.ContactEmail,
            WebsiteUrl = ad.WebsiteUrl,
            WhatsAppNumber = ad.WhatsAppNumber,
            OpeningHours = ad.OpeningHours,
            Status = ad.Status,
            ModerationNote = ad.ModerationNote,
            ExistingPhotos = ad.Photos.OrderBy(p => p.SortOrder).ToList(),
            Categories = await ActiveCategoriesAsync(ct),
            Services = ad.ServiceOfferings.OrderBy(s => s.SortOrder).Select(s => new ServiceOfferingInput
            {
                Name = s.Name,
                Details = s.Details,
                PriceFrom = s.PriceFrom,
                PriceUnit = s.PriceUnit
            }).ToList()
        };

        return View(model);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(40 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, AdvertisementFormViewModel model, CancellationToken ct)
    {
        var ad = await OwnedAdAsync(id, ct);
        if (ad is null) return NotFound();

        model.Services = model.Services.Where(s => !string.IsNullOrWhiteSpace(s.Name)).ToList();
        if (model.Services.Count == 0)
            ModelState.AddModelError(nameof(model.Services), "Add at least one service offering.");

        var keptPhotos = ad.Photos.Count(p => !model.RemovePhotoIds.Contains(p.Id));
        ValidatePhotos(model.NewPhotos, keptPhotos);

        if (!ModelState.IsValid)
        {
            model.Id = ad.Id;
            model.ExistingPhotos = ad.Photos.OrderBy(p => p.SortOrder).ToList();
            model.Categories = await ActiveCategoriesAsync(ct);
            if (model.Services.Count == 0) model.Services.Add(new ServiceOfferingInput());
            return View(model);
        }

        ad.BusinessName = model.BusinessName;
        ad.Slug = SlugBuilder.Create(model.BusinessName);
        ad.Description = model.Description;
        ad.CategoryId = model.CategoryId;
        ad.AddressLine = model.AddressLine;
        ad.Locality = model.Locality;
        ad.City = model.City;
        ad.PostalCode = model.PostalCode;
        ad.ContactPhone = model.ContactPhone;
        ad.ContactEmail = model.ContactEmail;
        ad.WebsiteUrl = model.WebsiteUrl;
        ad.WhatsAppNumber = model.WhatsAppNumber;
        ad.OpeningHours = model.OpeningHours;
        ad.UpdatedUtc = DateTime.UtcNow;
        // Any edit re-enters the moderation queue.
        ad.Status = AdStatus.Pending;
        ad.ModerationNote = null;

        _db.ServiceOfferings.RemoveRange(ad.ServiceOfferings);
        for (var i = 0; i < model.Services.Count; i++)
        {
            var s = model.Services[i];
            ad.ServiceOfferings.Add(new ServiceOffering
            {
                Name = s.Name.Trim(),
                Details = s.Details,
                PriceFrom = s.PriceFrom,
                PriceUnit = s.PriceUnit,
                SortOrder = i + 1
            });
        }

        foreach (var photo in ad.Photos.Where(p => model.RemovePhotoIds.Contains(p.Id)).ToList())
        {
            if (!string.IsNullOrEmpty(photo.StorageKey))
                await _photos.DeleteAsync(photo.StorageKey, ct);
            _db.AdPhotos.Remove(photo);
        }

        await SavePhotosAsync(ad, model.NewPhotos, ct);
        await _db.SaveChangesAsync(ct);

        TempData["Toast"] = "Listing updated and resubmitted for review.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var ad = await OwnedAdAsync(id, ct);
        if (ad is null) return NotFound();

        foreach (var photo in ad.Photos.Where(p => !string.IsNullOrEmpty(p.StorageKey)))
            await _photos.DeleteAsync(photo.StorageKey!, ct);

        _db.Advertisements.Remove(ad);
        await _db.SaveChangesAsync(ct);

        TempData["Toast"] = "Listing deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<Advertisement?> OwnedAdAsync(int id, CancellationToken ct)
    {
        var userId = User.UserId();
        return await _db.Advertisements
            .Include(a => a.Photos)
            .Include(a => a.ServiceOfferings)
            .FirstOrDefaultAsync(a => a.Id == id && a.OwnerId == userId, ct);
    }

    private Task<List<Category>> ActiveCategoriesAsync(CancellationToken ct)
        => _db.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync(ct);

    private void ValidatePhotos(IEnumerable<IFormFile> files, int existingCount)
    {
        var list = files?.Where(f => f is not null && f.Length > 0).ToList() ?? new List<IFormFile>();
        if (existingCount + list.Count > PhotoValidator.MaxPhotosPerAd)
            ModelState.AddModelError(nameof(AdvertisementFormViewModel.NewPhotos),
                $"A listing can hold at most {PhotoValidator.MaxPhotosPerAd} photos.");

        foreach (var file in list)
        {
            if (!PhotoValidator.IsAllowed(file, out var error))
                ModelState.AddModelError(nameof(AdvertisementFormViewModel.NewPhotos), error);
        }
    }

    private async Task SavePhotosAsync(Advertisement ad, IEnumerable<IFormFile> files, CancellationToken ct)
    {
        var incoming = files?.Where(f => f is not null && f.Length > 0).ToList() ?? new List<IFormFile>();
        if (incoming.Count == 0) return;

        var nextOrder = ad.Photos.Count == 0 ? 1 : ad.Photos.Max(p => p.SortOrder) + 1;
        var hasPrimary = ad.Photos.Any(p => p.IsPrimary);

        foreach (var file in incoming)
        {
            await using var stream = file.OpenReadStream();
            var stored = await _photos.SaveAsync(stream, file.FileName, file.ContentType, ct);

            ad.Photos.Add(new AdPhoto
            {
                Url = stored.Url,
                StorageKey = stored.StorageKey,
                ContentType = stored.ContentType,
                SizeBytes = stored.SizeBytes,
                SortOrder = nextOrder++,
                IsPrimary = !hasPrimary
            });
            hasPrimary = true;
        }
    }
}
