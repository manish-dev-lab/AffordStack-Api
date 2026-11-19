using SoftwareServices.Domain.Enums;

namespace SoftwareServices.Application.DTOs.Quotation;

public class CreateQuotationRequest
{
    public int EnquiryId { get; set; }
    public string ServiceDescription { get; set; } = string.Empty;
    public string Deliverables { get; set; } = string.Empty;

    public decimal DevelopmentCost { get; set; }
    public decimal AdditionalCost { get; set; } = 0;
    public decimal TaxRate { get; set; } = 0;
    public decimal AdvancePaymentAmount { get; set; }
    public DateTime ExpectedDeliveryDate { get; set; }
    public int ValidityDays { get; set; } = 15;
    public string TermsAndConditions { get; set; } = string.Empty;

    public List<QuotationItemDto> Items { get; set; } = new();
}

public class UpdateQuotationRequest
{
    public string ServiceDescription { get; set; } = string.Empty;
    public string Deliverables { get; set; } = string.Empty;

    public decimal DevelopmentCost { get; set; }
    public decimal AdditionalCost { get; set; } = 0;
    public decimal TaxRate { get; set; } = 0;
    public decimal AdvancePaymentAmount { get; set; }
    public DateTime ExpectedDeliveryDate { get; set; }
    public int ValidityDays { get; set; } = 15;
    public string TermsAndConditions { get; set; } = string.Empty;

    public List<QuotationItemDto> Items { get; set; } = new();
}

public class QuotationItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class QuotationDto
{
    public int Id { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public int EnquiryId { get; set; }
    public string EnquiryReferenceNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }

    public string ServiceDescription { get; set; } = string.Empty;
    public string Deliverables { get; set; } = string.Empty;

    public decimal DevelopmentCost { get; set; }
    public decimal AdditionalCost { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AdvancePaymentAmount { get; set; }
    public decimal RemainingPaymentAmount { get; set; }

    public DateTime ExpectedDeliveryDate { get; set; }
    public int ValidityDays { get; set; }
    public DateTime ValidUntil { get; set; }
    public string TermsAndConditions { get; set; } = string.Empty;

    public QuotationStatus Status { get; set; }
    public string SecureToken { get; set; } = string.Empty;
    public DateTime? SentAt { get; set; }
    public DateTime? ViewedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? ResponseRemarks { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<QuotationItemDto> Items { get; set; } = new();
    public int? ProjectId { get; set; }
}

public class PublicQuotationDto
{
    public string QuotationNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string ServiceDescription { get; set; } = string.Empty;
    public string Deliverables { get; set; } = string.Empty;

    public decimal DevelopmentCost { get; set; }
    public decimal AdditionalCost { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AdvancePaymentAmount { get; set; }
    public decimal RemainingPaymentAmount { get; set; }

    public DateTime ExpectedDeliveryDate { get; set; }
    public DateTime ValidUntil { get; set; }
    public string TermsAndConditions { get; set; } = string.Empty;

    public QuotationStatus Status { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? ResponseRemarks { get; set; }

    public List<QuotationItemDto> Items { get; set; } = new();
}

public class QuotationDecisionRequest
{
    public string? Remarks { get; set; }
}
