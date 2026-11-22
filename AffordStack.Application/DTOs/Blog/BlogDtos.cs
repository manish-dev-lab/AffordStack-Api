namespace AffordStack.Application.DTOs.Blog;

public class BlogCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PostCount { get; set; }
}

public class BlogPostDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? BlogCategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CreateBlogPostRequest
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? BlogCategoryId { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string AuthorName { get; set; } = "Technical Team";
    public bool IsPublished { get; set; } = true;
    public List<string> Tags { get; set; } = new();
}

public class UpdateBlogPostRequest : CreateBlogPostRequest
{
}
