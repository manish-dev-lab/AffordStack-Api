using AffordStack.Application.DTOs.Payment;

namespace AffordStack.Application.Interfaces;

public interface ICashfreeService
{
    string Environment { get; }
    Task<CashfreeOrderResponse> CreateOrderAsync(
        string orderId,
        decimal amount,
        string customerId,
        string customerName,
        string customerEmail,
        string customerPhone,
        string orderNote,
        string? returnUrl = null);

    Task<PaymentVerificationResponse> GetOrderDetailsAsync(string orderId);

    bool VerifyWebhookSignature(string rawBody, string signature, string timestamp);
}
