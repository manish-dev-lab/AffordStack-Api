using AffordStack.Domain.Common;

namespace AffordStack.Domain.Entities;

public class BlogPost : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public int? BlogCategoryId { get; set; }
    public BlogCategory? BlogCategory { get; set; }

    public string? ThumbnailUrl { get; set; }
    public string AuthorName { get; set; } = "Technical Team";
    public bool IsPublished { get; set; } = true;
    public DateTime? PublishedAt { get; set; } = DateTime.UtcNow;
    public int ViewCount { get; set; } = 0;
    public string TagsJson { get; set; } = "[]";
}
