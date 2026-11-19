using SoftwareServices.Domain.Enums;

namespace SoftwareServices.Application.DTOs.Project;

public class CreateProjectRequest
{
    public int? QuotationId { get; set; }
    public int CustomerId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime ExpectedCompletionDate { get; set; }
    public decimal AgreedAmount { get; set; }
    public string? AssignedDeveloper { get; set; }
    public string? InternalNotes { get; set; }

    public List<CreateProjectMilestoneDto> Milestones { get; set; } = new();
}

public class UpdateProjectRequest
{
    public string ProjectTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ExpectedCompletionDate { get; set; }
    public decimal AgreedAmount { get; set; }
    public ProjectStatus Status { get; set; }
    public int ProgressPercentage { get; set; }
    public string? AssignedDeveloper { get; set; }
    public string? InternalNotes { get; set; }
}

public class CreateProjectMilestoneDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
}

public class ProjectMilestoneDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal Amount { get; set; }
}

public class ProjectDto
{
    public int Id { get; set; }
    public string ProjectNumber { get; set; } = string.Empty;
    public int? QuotationId { get; set; }
    public string? QuotationNumber { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    public string ProjectTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime ExpectedCompletionDate { get; set; }
    public DateTime? ActualCompletionDate { get; set; }

    public decimal AgreedAmount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal OutstandingBalance => AgreedAmount - TotalPaid;

    public ProjectStatus Status { get; set; }
    public int ProgressPercentage { get; set; }
    public string? AssignedDeveloper { get; set; }
    public string? InternalNotes { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<ProjectMilestoneDto> Milestones { get; set; } = new();
}
