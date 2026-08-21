using YellowPages.Web.Models;

namespace YellowPages.Web.ViewModels;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public int TotalCount { get; init; }
    public int TotalPages => PageSize == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public class DirectorySearchViewModel
{
    public string? Query { get; set; }
    public string? CategorySlug { get; set; }
    public string? Locality { get; set; }
    public string Sort { get; set; } = "relevance";
    public int Page { get; set; } = 1;

    public PagedResult<Advertisement> Results { get; set; } = new();
    public IEnumerable<Category> Categories { get; set; } = new List<Category>();
    public IEnumerable<string> Localities { get; set; } = new List<string>();
}

public class HomeViewModel
{
    public IEnumerable<Category> Categories { get; set; } = new List<Category>();
    public IEnumerable<Advertisement> FeaturedAds { get; set; } = new List<Advertisement>();
    public IEnumerable<Advertisement> RecentAds { get; set; } = new List<Advertisement>();
    public IEnumerable<string> Localities { get; set; } = new List<string>();
    public int TotalListings { get; set; }
}

public class AdminAdListViewModel
{
    public string? Query { get; set; }
    public AdStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public PagedResult<Advertisement> Results { get; set; } = new();
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int SuspendedCount { get; set; }
}
