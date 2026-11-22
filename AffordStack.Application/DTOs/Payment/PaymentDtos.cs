using AffordStack.Domain.Enums;

namespace AffordStack.Application.DTOs.Payment;

public class RecordPaymentRequest
{
    public int? ProjectId { get; set; }
    public int? QuotationId { get; set; }
    public PaymentType PaymentType { get; set; } = PaymentType.Advance;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Bank Transfer";
    public string? TransactionReference { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Received;
    public string? Notes { get; set; }
}

public class PaymentDto
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectNumber { get; set; }
    public string? ProjectTitle { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int? QuotationId { get; set; }
    public string? QuotationNumber { get; set; }
    public string? CashfreeOrderId { get; set; }
    public string? CashfreePaymentId { get; set; }

    public PaymentType PaymentType { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public PaymentStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
