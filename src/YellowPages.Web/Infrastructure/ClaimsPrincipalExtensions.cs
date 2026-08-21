using System.Security.Claims;

namespace YellowPages.Web.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Reads the "sub" claim written by JwtTokenService.</summary>
    public static string? UserId(this ClaimsPrincipal principal)
        => principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public static string DisplayName(this ClaimsPrincipal principal)
        => principal.FindFirst("name")?.Value
           ?? principal.FindFirst("email")?.Value
           ?? "Member";
}
