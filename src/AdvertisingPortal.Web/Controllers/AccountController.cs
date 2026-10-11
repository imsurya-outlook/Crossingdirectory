using AdvertisingPortal.Domain.Common;
using AdvertisingPortal.Domain.Entities;
using AdvertisingPortal.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AdvertisingPortal.Web.Controllers;

[Route("Account")]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    private bool GoogleEnabled =>
        !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientSecret"]);

    [HttpGet("Login")]
    public IActionResult Login(string? returnUrl = null)
        => View(new LoginViewModel { ReturnUrl = returnUrl, GoogleEnabled = GoogleEnabled });

    [HttpPost("Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        model.GoogleEnabled = GoogleEnabled;
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null && await _userManager.IsInRoleAsync(user, RoleNames.Admin))
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

        return RedirectToAction("Index", "Dashboard", new { area = "Advertiser" });
    }

    [HttpGet("Register")]
    public IActionResult Register()
        => View(new RegisterViewModel { GoogleEnabled = GoogleEnabled });

    [HttpPost("Register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        model.GoogleEnabled = GoogleEnabled;
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true
        };

        var created = await _userManager.CreateAsync(user, model.Password);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        await _userManager.AddToRoleAsync(user, RoleNames.Advertiser);
        await _signInManager.SignInAsync(user, isPersistent: true);

        return RedirectToAction("Index", "Dashboard", new { area = "Advertiser" });
    }

    // ---- Google sign-in ----

    [HttpPost("ExternalLogin")]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        if (!GoogleEnabled) return RedirectToAction(nameof(Login));

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet("ExternalLoginCallback")]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError is not null)
        {
            TempData["Error"] = "Google sign-in failed: " + remoteError;
            return RedirectToAction(nameof(Login));
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            TempData["Error"] = "Could not read the details returned by Google. Please try again.";
            return RedirectToAction(nameof(Login));
        }

        // Already linked: sign straight in.
        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            var linked = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            return await RedirectAfterSignInAsync(linked);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Your Google account did not share an email address, so an account could not be created.";
            return RedirectToAction(nameof(Login));
        }

        var emailVerified = string.Equals(
            info.Principal.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase);

        // An account may already exist for this email. Link it only when Google verified the address,
        // so an unverified Google email cannot take over an existing account.
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!emailVerified)
            {
                TempData["Error"] = "That email is already registered here. Please sign in with your password instead.";
                return RedirectToAction(nameof(Login));
            }

            var linkResult = await _userManager.AddLoginAsync(existing, info);
            if (!linkResult.Succeeded)
            {
                TempData["Error"] = "Could not link your Google account to the existing account.";
                return RedirectToAction(nameof(Login));
            }

            await _signInManager.SignInAsync(existing, isPersistent: true);
            TempData["Success"] = "Your Google account is now linked to this account.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return await RedirectAfterSignInAsync(existing);
        }

        // Brand new user via Google: no password is set, so only Google sign-in works for them.
        var fullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email;
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        var created = await _userManager.CreateAsync(user);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors)
                TempData["Error"] = error.Description;

            return RedirectToAction(nameof(Login));
        }

        await _userManager.AddLoginAsync(user, info);
        await _userManager.AddToRoleAsync(user, RoleNames.Advertiser);
        await _signInManager.SignInAsync(user, isPersistent: true);

        TempData["Success"] = "Welcome! Your account was created with Google.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return await RedirectAfterSignInAsync(user);
    }

    private async Task<IActionResult> RedirectAfterSignInAsync(ApplicationUser? user)
    {
        if (user is not null && await _userManager.IsInRoleAsync(user, RoleNames.Admin))
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

        return RedirectToAction("Index", "Dashboard", new { area = "Advertiser" });
    }

    [HttpPost("Logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("AccessDenied")]
    public IActionResult AccessDenied() => View();
}
