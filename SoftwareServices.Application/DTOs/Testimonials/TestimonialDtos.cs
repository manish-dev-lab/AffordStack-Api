namespace SoftwareServices.Application.DTOs.Testimonials;

public class TestimonialDto
{
    public int Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientRole { get; set; } = string.Empty;
    public string CompanyOrInstitution { get; set; } = string.Empty;
    public string Feedback { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? AvatarUrl { get; set; }
    public string? ProjectCategory { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTestimonialRequest
{
    public string ClientName { get; set; } = string.Empty;
    public string ClientRole { get; set; } = string.Empty;
    public string CompanyOrInstitution { get; set; } = string.Empty;
    public string Feedback { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
    public string? AvatarUrl { get; set; }
    public string? ProjectCategory { get; set; }
    public bool IsFeatured { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
}
