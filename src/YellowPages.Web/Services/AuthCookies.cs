using YellowPages.Web.Services;

namespace YellowPages.Web.Infrastructure;

/// <summary>
/// The JWT is delivered to the browser in a hardened HttpOnly cookie rather than
/// localStorage: MVC gets normal server-side redirects and the token is unreachable
/// from JavaScript, which removes the usual XSS token-theft path.
/// </summary>
public static class AuthCookies
{
    public const string AccessToken = "yp_at";
    public const string RefreshToken = "yp_rt";

    public static void Write(HttpResponse response, AuthTokens tokens, bool secure = true)
    {
        response.Cookies.Append(AccessToken, tokens.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = tokens.RefreshTokenExpiresUtc,
            Path = "/"
        });

        response.Cookies.Append(RefreshToken, tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            Expires = tokens.RefreshTokenExpiresUtc,
            Path = "/"
        });
    }

    public static void Clear(HttpResponse response)
    {
        response.Cookies.Delete(AccessToken, new CookieOptions { Path = "/" });
        response.Cookies.Delete(RefreshToken, new CookieOptions { Path = "/" });
    }
}
