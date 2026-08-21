using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using YellowPages.Web.Data;
using YellowPages.Web.Models;
using YellowPages.Web.Services;
using YellowPages.Web.ViewModels;

namespace YellowPages.Web.Controllers.Api;

/// <summary>
/// Token endpoints for non-browser clients (a future mobile app or SPA).
/// The MVC site uses the same JWTs, delivered in HttpOnly cookies.
/// </summary>
[ApiController]
[Route("api/auth")]
[IgnoreAntiforgeryToken]
public class AuthApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly ITokenService _tokens;

    public AuthApiController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, ITokenService tokens)
    {
        _users = users;
        _signIn = signIn;
        _tokens = tokens;
    }

    public record LoginRequest(string Identifier, string Password);
    public record RefreshRequest(string RefreshToken);

    [HttpPost("register"), AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (await _users.FindByEmailAsync(model.Email) is not null)
            return Conflict(new { message = "An account already exists for this email address." });

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            PhoneNumber = model.MobileNumber,
            FullName = model.FullName,
            Locality = model.Locality
        };

        var created = await _users.CreateAsync(user, model.Password);
        if (!created.Succeeded)
            return BadRequest(new { errors = created.Errors.Select(e => e.Description) });

        await _users.AddToRoleAsync(user, AppRoles.Member);
        var tokens = await _tokens.IssueAsync(user, Ip(), ct);
        return Ok(tokens);
    }

    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = request.Identifier.Contains('@')
            ? await _users.FindByEmailAsync(request.Identifier)
            : _users.Users.FirstOrDefault(u => u.PhoneNumber == request.Identifier);

        if (user is null) return Unauthorized(new { message = "Invalid credentials." });

        var check = await _signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (check.IsLockedOut) return StatusCode(423, new { message = "Account temporarily locked." });
        if (!check.Succeeded) return Unauthorized(new { message = "Invalid credentials." });

        var tokens = await _tokens.IssueAsync(user, Ip(), ct);
        return Ok(tokens);
    }

    [HttpPost("refresh"), AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var tokens = await _tokens.RefreshAsync(request.RefreshToken, Ip(), ct);
        return tokens is null
            ? Unauthorized(new { message = "Refresh token is invalid, expired, or already used." })
            : Ok(tokens);
    }

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct)
    {
        await _tokens.RevokeAsync(request.RefreshToken, ct);
        return NoContent();
    }

    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
