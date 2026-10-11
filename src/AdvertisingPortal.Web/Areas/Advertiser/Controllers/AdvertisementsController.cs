using AdvertisingPortal.Application.Common;
using AdvertisingPortal.Application.DTOs;
using AdvertisingPortal.Application.Interfaces;
using AdvertisingPortal.Domain.Common;
using AdvertisingPortal.Domain.Enums;
using AdvertisingPortal.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace AdvertisingPortal.Web.Areas.Advertiser.Controllers;

[Area("Advertiser")]
[Authorize(Roles = RoleNames.Advertiser + "," + RoleNames.Admin)]
public class AdvertisementsController : Controller
{
    private readonly IAdvertisementService _ads;
    private readonly ICategoryService _categories;
    private readonly ILocalityService _localities;
    private readonly IFileStorageService _storage;

    public AdvertisementsController(
        IAdvertisementService ads,
        ICategoryService categories,
        ILocalityService localities,
        IFileStorageService storage)
    {
        _ads = ads;
        _categories = categories;
        _localities = localities;
        _storage = storage;
    }

    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
    private bool IsAdmin => User.IsInRole(RoleNames.Admin);

    public async Task<IActionResult> Index()
        => View(await _ads.GetAdvertiserAdvertisementsAsync(UserId));

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new AdvertisementFormViewModel
        {
            LocalityId = await _localities.GetDefaultLocalityIdAsync() ?? 0
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(60 * 1024 * 1024)]
    public async Task<IActionResult> Create(AdvertisementFormViewModel model, string submitAction)
    {
        ValidateImages(model.Images, 0);

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var uploads = await ToUploadDtosAsync(model.Images);
        var submitForApproval = submitAction != "draft";

        var id = await _ads.CreateAsync(new AdvertisementCreateDto
        {
            Title = model.Title,
            Description = model.Description,
            ContactName = model.ContactName,
            PhoneNumber = model.PhoneNumber,
            Email = model.Email,
            WebsiteUrl = model.WebsiteUrl,
            AddressLine = model.AddressLine,
            CategoryId = model.CategoryId,
            LocalityId = model.LocalityId
        }, uploads, UserId, submitForApproval);

        TempData["Success"] = submitForApproval
            ? "Advertisement submitted for approval."
            : "Advertisement saved as draft.";

        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var ad = await _ads.GetOwnedAsync(id, UserId, IsAdmin);
        if (ad is null) return NotFound();

        var model = new AdvertisementFormViewModel
        {
            Id = ad.Id,
            Title = ad.Title,
            Description = ad.Description,
            ContactName = ad.ContactName,
            PhoneNumber = ad.PhoneNumber,
            Email = ad.Email,
            WebsiteUrl = ad.WebsiteUrl,
            AddressLine = ad.AddressLine,
            CategoryId = ad.CategoryId,
            LocalityId = ad.LocalityId,
            Status = ad.Status,
            ExistingImages = ad.Images
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.DisplayOrder)
                .Select(i => new AdvertisementImageDto
                {
                    Id = i.Id,
                    ImageUrl = _storage.GetPublicUrl(i.FilePath),
                    IsPrimary = i.IsPrimary,
                    DisplayOrder = i.DisplayOrder
                }).ToList()
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(60 * 1024 * 1024)]
    public async Task<IActionResult> Edit(AdvertisementFormViewModel model, string submitAction)
    {
        var existing = await _ads.GetOwnedAsync(model.Id, UserId, IsAdmin);
        if (existing is null) return NotFound();

        ValidateImages(model.Images, existing.Images.Count);

        if (!ModelState.IsValid)
        {
            model.ExistingImages = existing.Images
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.DisplayOrder)
                .Select(i => new AdvertisementImageDto
                {
                    Id = i.Id,
                    ImageUrl = _storage.GetPublicUrl(i.FilePath),
                    IsPrimary = i.IsPrimary,
                    DisplayOrder = i.DisplayOrder
                }).ToList();

            await PopulateOptionsAsync(model);
            return View(model);
        }

        var uploads = await ToUploadDtosAsync(model.Images);
        var submitForApproval = submitAction != "draft";

        await _ads.UpdateAsync(new AdvertisementUpdateDto
        {
            Id = model.Id,
            Title = model.Title,
            Description = model.Description,
            ContactName = model.ContactName,
            PhoneNumber = model.PhoneNumber,
            Email = model.Email,
            WebsiteUrl = model.WebsiteUrl,
            AddressLine = model.AddressLine,
            CategoryId = model.CategoryId,
            LocalityId = model.LocalityId
        }, uploads, UserId, IsAdmin, submitForApproval);

        TempData["Success"] = submitForApproval
            ? "Advertisement updated and sent for approval."
            : "Advertisement changes saved.";

        return RedirectToAction(nameof(Edit), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int imageId, int advertisementId)
    {
        await _ads.DeleteImageAsync(imageId, UserId, IsAdmin);
        TempData["Success"] = "Image removed.";
        return RedirectToAction(nameof(Edit), new { id = advertisementId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPrimaryImage(int imageId, int advertisementId)
    {
        await _ads.SetPrimaryImageAsync(imageId, UserId, IsAdmin);
        TempData["Success"] = "Primary image updated.";
        return RedirectToAction(nameof(Edit), new { id = advertisementId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id)
    {
        await _ads.SubmitForApprovalAsync(id, UserId, IsAdmin);
        TempData["Success"] = "Advertisement submitted for approval.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateImages(List<IFormFile> images, int existingCount)
    {
        var files = images?.Where(f => f is not null && f.Length > 0).ToList() ?? new List<IFormFile>();

        if (existingCount + files.Count > UploadLimits.MaxImagesPerAdvertisement)
            ModelState.AddModelError(nameof(AdvertisementFormViewModel.Images),
                "You can keep a maximum of " + UploadLimits.MaxImagesPerAdvertisement + " images per advertisement.");

        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!UploadLimits.AllowedExtensions.Contains(extension))
                ModelState.AddModelError(nameof(AdvertisementFormViewModel.Images),
                    file.FileName + " is not a supported image type (JPG, PNG or WebP).");

            if (file.Length > UploadLimits.MaxImageSizeBytes)
                ModelState.AddModelError(nameof(AdvertisementFormViewModel.Images),
                    file.FileName + " is larger than 5 MB.");
        }
    }

    private async Task<List<UploadedFileDto>> ToUploadDtosAsync(List<IFormFile> images)
    {
        var uploads = new List<UploadedFileDto>();
        if (images is null) return uploads;

        foreach (var file in images.Where(f => f is not null && f.Length > 0))
        {
            var memory = new MemoryStream();
            await file.CopyToAsync(memory);
            memory.Position = 0;

            uploads.Add(new UploadedFileDto
            {
                FileName = Path.GetFileName(file.FileName),
                ContentType = file.ContentType,
                Length = file.Length,
                Content = memory
            });
        }

        return uploads;
    }

    private async Task PopulateOptionsAsync(AdvertisementFormViewModel model)
    {
        model.CategoryOptions = (await _categories.GetActiveAsync())
            .Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == model.CategoryId)).ToList();

        model.LocalityOptions = (await _localities.GetActiveAsync())
            .Select(l => new SelectListItem(l.Name, l.Id.ToString(), l.Id == model.LocalityId)).ToList();
    }
}
