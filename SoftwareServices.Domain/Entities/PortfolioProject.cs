using SoftwareServices.Domain.Common;

namespace SoftwareServices.Domain.Entities;

public class PortfolioProject : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string DetailedDescription { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // e.g. "Student Project", "Web Application", "Enterprise"
    public string? ThumbnailUrl { get; set; }
    public string? LiveDemoUrl { get; set; }
    public string? GitHubUrl { get; set; }

    public string TechnologiesJson { get; set; } = "[]";
    public string FeaturesJson { get; set; } = "[]";

    public int DisplayOrder { get; set; } = 0;
    public bool IsFeatured { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
