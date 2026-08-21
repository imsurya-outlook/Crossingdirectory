using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using YellowPages.Web.Data;
using YellowPages.Web.Models;
using YellowPages.Web.Options;

namespace YellowPages.Web.Services;

/// <summary>
/// Issues short-lived signed JWT access tokens plus long-lived, hashed, single-use
/// refresh tokens with rotation. Refresh tokens are never stored in plain text.
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly JwtOptions _jwt;

    public JwtTokenService(AppDbContext db, UserManager<ApplicationUser> users, IOptions<JwtOptions> jwt)
    {
        _db = db;
        _users = users;
        _jwt = jwt.Value;
    }

    public async Task<AuthTokens> IssueAsync(ApplicationUser user, string? ip, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var accessExpires = now.AddMinutes(_jwt.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new("sub", user.Id),
            new("name", user.FullName),
            new("email", user.Email ?? string.Empty),
            new("mobile", user.PhoneNumber ?? string.Empty),
            new("jti", Guid.NewGuid().ToString("n"))
        };
        foreach (var role in await _users.GetRolesAsync(user))
            claims.Add(new Claim("role", role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = accessExpires,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };
        var accessToken = handler.CreateToken(descriptor);

        var (refreshPlain, refreshHash) = CreateRefreshToken();
        var refreshExpires = now.AddDays(_jwt.RefreshTokenDays);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresUtc = refreshExpires,
            CreatedUtc = now,
            CreatedByIp = ip
        });
        await _db.SaveChangesAsync(ct);

        return new AuthTokens(accessToken, accessExpires, refreshPlain, refreshExpires);
    }

    public async Task<AuthTokens?> RefreshAsync(string refreshToken, string? ip, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return null;

        var hash = Hash(refreshToken);
        var stored = await _db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == hash, ct);

        if (stored?.User is null) return null;

        // Reuse of an already-rotated token is treated as theft: kill the whole family.
        if (stored.RevokedUtc is not null)
        {
            await RevokeAllForUserAsync(stored.UserId, ct);
            return null;
        }

        if (DateTime.UtcNow >= stored.ExpiresUtc) return null;

        var issued = await IssueAsync(stored.User, ip, ct);
        stored.RevokedUtc = DateTime.UtcNow;
        stored.ReplacedByTokenHash = Hash(issued.RefreshToken);
        await _db.SaveChangesAsync(ct);

        return issued;
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var hash = Hash(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (stored is null || stored.RevokedUtc is not null) return;
        stored.RevokedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeAllForUserAsync(string userId, CancellationToken ct = default)
    {
        var active = await _db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedUtc == null)
            .ToListAsync(ct);
        foreach (var token in active) token.RevokedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static (string Plain, string Hash) CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var plain = Convert.ToBase64String(bytes);
        return (plain, Hash(plain));
    }

    private static string Hash(string value)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
