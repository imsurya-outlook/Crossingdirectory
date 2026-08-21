using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using YellowPages.Web.Data;
using YellowPages.Web.Infrastructure;
using YellowPages.Web.Models;
using YellowPages.Web.Services;
using YellowPages.Web.ViewModels;

namespace YellowPages.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly ITokenService _tokens;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        ITokenService tokens,
        IWebHostEnvironment env,
        ILogger<AccountController> logger)
    {
        _users = users;
        _signIn = signIn;
        _tokens = tokens;
        _env = env;
        _logger = logger;
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _users.FindByEmailAsync(model.Email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account already exists for this email address.");
            return View(model);
        }

        var mobileTaken = _users.Users.Any(u => u.PhoneNumber == model.MobileNumber);
        if (mobileTaken)
        {
            ModelState.AddModelError(nameof(model.MobileNumber), "An account already exists for this mobile number.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            PhoneNumber = model.MobileNumber,
            FullName = model.FullName,
            Locality = model.Locality
        };

        var result = await _users.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _users.AddToRoleAsync(user, AppRoles.Member);
        _logger.LogInformation("New member registered: {UserId}", user.Id);

        // Registration signs the member straight in by issuing a JWT pair.
        var tokens = await _tokens.IssueAsync(user, RemoteIp(), ct);
        AuthCookies.Write(Response, tokens, secure: !_env.IsDevelopment());

        TempData["Toast"] = "Welcome aboard. Your account is ready.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
        => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var user = model.Identifier.Contains('@')
            ? await _users.FindByEmailAsync(model.Identifier)
            : _users.Users.FirstOrDefault(u => u.PhoneNumber == model.Identifier);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Those credentials did not match an account.");
            return View(model);
        }

        // CheckPasswordSignInAsync applies the lockout policy without creating a cookie
        // session, which keeps JWT the single source of identity.
        var check = await _signIn.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if (check.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is temporarily locked after repeated failed attempts. Try again later.");
            return View(model);
        }
        if (!check.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Those credentials did not match an account.");
            return View(model);
        }

        user.LastLoginUtc = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        var tokens = await _tokens.IssueAsync(user, RemoteIp(), ct);
        AuthCookies.Write(Response, tokens, secure: !_env.IsDevelopment());

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (Request.Cookies.TryGetValue(AuthCookies.RefreshToken, out var refresh))
            await _tokens.RevokeAsync(refresh, ct);

        AuthCookies.Clear(Response);
        TempData["Toast"] = "You have been signed out.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet, Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _users.FindByIdAsync(User.UserId() ?? string.Empty);
        if (user is null) return RedirectToAction(nameof(Login));
        return View(user);
    }

    private string? RemoteIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
