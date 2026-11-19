namespace SoftwareServices.Application.DTOs.Services;

public class ServiceCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IconName { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public List<ServiceDto> Services { get; set; } = new();
}

public class ServiceDto
{
    public int Id { get; set; }
    public int ServiceCategoryId { get; set; }
    public string? ServiceCategoryName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new();
    public List<string> Technologies { get; set; } = new();
    public List<string> Benefits { get; set; } = new();
    public string? IconName { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
}

public class CreateServiceRequest
{
    public int ServiceCategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new();
    public List<string> Technologies { get; set; } = new();
    public List<string> Benefits { get; set; } = new();
    public string? IconName { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
}

public class UpdateServiceRequest : CreateServiceRequest
{
}
