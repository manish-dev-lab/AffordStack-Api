using SoftwareServices.Application.DTOs.Quotation;
using SoftwareServices.Domain.Enums;

namespace SoftwareServices.Application.DTOs.Enquiry;

public class CreateEnquiryRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CompanyName { get; set; }

    public int? ServiceCategoryId { get; set; }
    public string ServiceCategoryName { get; set; } = string.Empty;
    public string RequiredTechnology { get; set; } = string.Empty;
    public string ProjectTitle { get; set; } = string.Empty;
    public string ProjectDescription { get; set; } = string.Empty;
    public string EstimatedBudget { get; set; } = string.Empty;
    public DateTime? ExpectedCompletionDate { get; set; }
    public string? AdditionalRequirements { get; set; }
}

public class EnquiryResponseDto
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class EnquiryListDto
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? ServiceCategoryName { get; set; }
    public string RequiredTechnology { get; set; } = string.Empty;
    public string ProjectTitle { get; set; } = string.Empty;
    public string EstimatedBudget { get; set; } = string.Empty;
    public EnquiryStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int NotesCount { get; set; }
    public int QuotationsCount { get; set; }
}

public class EnquiryDetailDto
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }

    public int? ServiceCategoryId { get; set; }
    public string? ServiceCategoryName { get; set; }
    public string RequiredTechnology { get; set; } = string.Empty;
    public string ProjectTitle { get; set; } = string.Empty;
    public string ProjectDescription { get; set; } = string.Empty;
    public string EstimatedBudget { get; set; } = string.Empty;
    public DateTime? ExpectedCompletionDate { get; set; }
    public string? AdditionalRequirements { get; set; }
    public EnquiryStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<EnquiryAttachmentDto> Attachments { get; set; } = new();
    public List<EnquiryNoteDto> Notes { get; set; } = new();
}

public class EnquiryAttachmentDto
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EnquiryNoteDto
{
    public int Id { get; set; }
    public string? AuthorName { get; set; }
    public string NoteContent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpdateEnquiryStatusRequest
{
    public EnquiryStatus Status { get; set; }
    public string? Note { get; set; }
}

public class AddEnquiryNoteRequest
{
    public string NoteContent { get; set; } = string.Empty;
}

public class TrackEnquiryRequest
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class EnquiryTrackingDto
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string ProjectDescription { get; set; } = string.Empty;
    public string? ServiceCategoryName { get; set; }
    public string RequiredTechnology { get; set; } = string.Empty;
    public string EstimatedBudget { get; set; } = string.Empty;
    public EnquiryStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public string StatusDescription { get; set; } = string.Empty;
    public int StepIndex { get; set; } = 1; // 1: Submitted, 2: Under Review, 3: Quotation Ready, 4: Converted/In Progress, 5: Completed
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public PublicQuotationDto? ActiveQuotation { get; set; }

    // Linked project details if available
    public int? ProjectId { get; set; }
    public string? ProjectNumber { get; set; }
    public ProjectStatus? ProjectStatus { get; set; }
    public int? ProgressPercentage { get; set; }
    public DateTime? ExpectedCompletionDate { get; set; }
    public List<SoftwareServices.Application.DTOs.Project.ProjectMilestoneDto> Milestones { get; set; } = new();
}
