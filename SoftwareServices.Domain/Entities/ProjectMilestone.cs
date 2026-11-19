using SoftwareServices.Domain.Common;

namespace SoftwareServices.Domain.Entities;

public class ProjectMilestone : BaseEntity
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
    public decimal Amount { get; set; } = 0;
}
