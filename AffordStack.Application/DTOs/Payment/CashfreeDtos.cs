using AffordStack.Domain.Enums;

namespace AffordStack.Application.DTOs.Payment;

public class CreatePaymentOrderRequest
{
    public int? QuotationId { get; set; }
    public int? MilestoneId { get; set; }
    public string? SecureToken { get; set; }
    public string? ReturnUrl { get; set; }
}

public class CashfreeOrderResponse
{
    public string OrderId { get; set; } = string.Empty;
    public string? PaymentSessionId { get; set; }
    public decimal OrderAmount { get; set; }
    public string OrderCurrency { get; set; } = "INR";
    public string OrderStatus { get; set; } = string.Empty;
    public string Environment { get; set; } = "SANDBOX";
}

public class PaymentVerificationResponse
{
    public bool Success { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string? PaymentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
}

public class CustomerPaymentDto
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectNumber { get; set; }
    public string? ProjectTitle { get; set; }
    public int? QuotationId { get; set; }
    public string? QuotationNumber { get; set; }
    public PaymentType PaymentType { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public string? CashfreeOrderId { get; set; }
    public string? CashfreePaymentId { get; set; }
    public PaymentStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
