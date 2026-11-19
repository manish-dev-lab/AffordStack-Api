using SoftwareServices.Domain.Common;
using SoftwareServices.Domain.Enums;

namespace SoftwareServices.Domain.Entities;

public class Enquiry : BaseEntity
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? ServiceCategoryId { get; set; }
    public ServiceCategory? ServiceCategory { get; set; }

    public string RequiredTechnology { get; set; } = string.Empty;
    public string ProjectTitle { get; set; } = string.Empty;
    public string ProjectDescription { get; set; } = string.Empty;
    public string EstimatedBudget { get; set; } = string.Empty;
    public DateTime? ExpectedCompletionDate { get; set; }
    public string? AdditionalRequirements { get; set; }

    public EnquiryStatus Status { get; set; } = EnquiryStatus.New;

    public ICollection<EnquiryAttachment> Attachments { get; set; } = new List<EnquiryAttachment>();
    public ICollection<EnquiryNote> Notes { get; set; } = new List<EnquiryNote>();
    public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();
}
