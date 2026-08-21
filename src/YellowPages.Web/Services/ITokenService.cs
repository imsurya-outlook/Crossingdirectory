using YellowPages.Web.Models;

namespace YellowPages.Web.Services;

public interface ITokenService
{
    Task<AuthTokens> IssueAsync(ApplicationUser user, string? ip, CancellationToken ct = default);
    Task<AuthTokens?> RefreshAsync(string refreshToken, string? ip, CancellationToken ct = default);
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeAllForUserAsync(string userId, CancellationToken ct = default);
}
