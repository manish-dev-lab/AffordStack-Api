using AffordStack.Domain.Common;
using AffordStack.Domain.Enums;

namespace AffordStack.Domain.Entities;

public class Project : BaseEntity
{
    public string ProjectNumber { get; set; } = string.Empty;
    
    public int? QuotationId { get; set; }
    public Quotation? Quotation { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string ProjectTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime ExpectedCompletionDate { get; set; }
    public DateTime? ActualCompletionDate { get; set; }

    public decimal AgreedAmount { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.NotStarted;
    public int ProgressPercentage { get; set; } = 0;

    public string? AssignedDeveloper { get; set; }
    public string? InternalNotes { get; set; }

    public ICollection<ProjectMilestone> Milestones { get; set; } = new List<ProjectMilestone>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
