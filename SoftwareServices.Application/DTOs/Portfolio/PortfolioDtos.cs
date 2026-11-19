namespace SoftwareServices.Application.DTOs.Portfolio;

public class PortfolioProjectDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string DetailedDescription { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? LiveDemoUrl { get; set; }
    public string? GitHubUrl { get; set; }
    public List<string> Technologies { get; set; } = new();
    public List<string> Features { get; set; } = new();
    public int DisplayOrder { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePortfolioRequest
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string DetailedDescription { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? LiveDemoUrl { get; set; }
    public string? GitHubUrl { get; set; }
    public List<string> Technologies { get; set; } = new();
    public List<string> Features { get; set; } = new();
    public int DisplayOrder { get; set; }
    public bool IsFeatured { get; set; } = false;
    public bool IsActive { get; set; } = true;
}

public class UpdatePortfolioRequest : CreatePortfolioRequest
{
}
