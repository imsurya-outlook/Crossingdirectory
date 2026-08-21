using System.Text;
using System.Text.RegularExpressions;

namespace YellowPages.Web.Services;

public static class SlugBuilder
{
    public static string Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Guid.NewGuid().ToString("n")[..8];

        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (char.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch);
        }

        var slug = Regex.Replace(sb.ToString(), "[^a-z0-9]+", "-").Trim('-');
        if (slug.Length > 120) slug = slug[..120].Trim('-');
        return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("n")[..8] : slug;
    }
}
