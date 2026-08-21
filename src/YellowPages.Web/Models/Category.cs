namespace YellowPages.Web.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    /// <summary>Bootstrap Icons class, e.g. "bi-tools".</summary>
    public string Icon { get; set; } = "bi-shop";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Advertisement> Advertisements { get; set; } = new List<Advertisement>();
}
