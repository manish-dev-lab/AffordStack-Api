using AffordStack.Domain.Common;
using AffordStack.Domain.Enums;

namespace AffordStack.Domain.Entities;

public class Quotation : BaseEntity
{
    public string QuotationNumber { get; set; } = string.Empty;
    public int EnquiryId { get; set; }
    public Enquiry? Enquiry { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string ServiceDescription { get; set; } = string.Empty;
    public string Deliverables { get; set; } = string.Empty;

    public decimal DevelopmentCost { get; set; }
    public decimal AdditionalCost { get; set; } = 0;
    public decimal TaxRate { get; set; } = 0; // percentage e.g. 18.00
    public decimal TaxAmount { get; set; } = 0;
    public decimal TotalAmount { get; set; }
    public decimal AdvancePaymentAmount { get; set; }
    public decimal RemainingPaymentAmount { get; set; }

    public DateTime ExpectedDeliveryDate { get; set; }
    public int ValidityDays { get; set; } = 15;
    public DateTime ValidUntil { get; set; }
    public string TermsAndConditions { get; set; } = string.Empty;

    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;
    public string SecureToken { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime? SentAt { get; set; }
    public DateTime? ViewedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? ResponseRemarks { get; set; }

    public ICollection<QuotationItem> Items { get; set; } = new List<QuotationItem>();
    public ICollection<QuotationStatusHistory> StatusHistory { get; set; } = new List<QuotationStatusHistory>();
    public Project? Project { get; set; }
}
