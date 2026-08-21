namespace YellowPages.Web.Options;

public class JwtOptions
{
    public string Issuer { get; set; } = "https://localhost";
    public string Audience { get; set; } = "yellowpages-local-web";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 20;
    public int RefreshTokenDays { get; set; } = 14;
}
