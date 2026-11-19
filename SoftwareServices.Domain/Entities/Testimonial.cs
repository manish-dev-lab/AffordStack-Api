using SoftwareServices.Domain.Common;

namespace SoftwareServices.Domain.Entities;

public class Testimonial : BaseEntity
{
    public string ClientName { get; set; } = string.Empty;
    public string ClientRole { get; set; } = string.Empty; // e.g., "M.Tech Student, IIT", "CTO, RetailHub"
    public string CompanyOrInstitution { get; set; } = string.Empty;
    public string Feedback { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
    public string? AvatarUrl { get; set; }
    public string? ProjectCategory { get; set; } // "Student Project", "Enterprise", "Technical Support"
    public bool IsFeatured { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
}
