using SoftwareServices.Domain.Enums;

namespace SoftwareServices.Application.DTOs.Payment;

public class RecordPaymentRequest
{
    public int ProjectId { get; set; }
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
    public int ProjectId { get; set; }
    public string ProjectNumber { get; set; } = string.Empty;
    public string ProjectTitle { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    public PaymentType PaymentType { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public PaymentStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
