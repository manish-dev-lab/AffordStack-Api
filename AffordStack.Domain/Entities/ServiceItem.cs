using AffordStack.Domain.Common;

namespace AffordStack.Domain.Entities;

public class ServiceItem : BaseEntity
{
    public int ServiceCategoryId { get; set; }
    public ServiceCategory? ServiceCategory { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    
    // JSON arrays stored as string
    public string FeaturesJson { get; set; } = "[]";
    public string TechnologiesJson { get; set; } = "[]";
    public string BenefitsJson { get; set; } = "[]";

    public string? IconName { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
}
