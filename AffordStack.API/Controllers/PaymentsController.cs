using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AffordStack.Application.Common;
using AffordStack.Application.DTOs.Payment;
using AffordStack.Application.Interfaces;

namespace AffordStack.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ICashfreeService _cashfreeService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        ICashfreeService cashfreeService,
        IApplicationDbContext context,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _cashfreeService = cashfreeService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a Cashfree payment order for quotation advance amount (Public via secure quotation token)
    /// </summary>
    [HttpPost("quotations/{secureToken}/create-order")]
    public async Task<ActionResult<ApiResponse<CashfreeOrderResponse>>> CreateQuotationAdvanceOrder(
        string secureToken,
        [FromBody] CreatePaymentOrderRequest? request)
    {
        var result = await _paymentService.CreateQuotationAdvanceOrderAsync(secureToken, request?.ReturnUrl);
        return Ok(ApiResponse<CashfreeOrderResponse>.Ok(result, "Cashfree payment session created successfully."));
    }

    /// <summary>
    /// Create a Cashfree payment order for a project milestone (Authenticated customer only)
    /// </summary>
    [Authorize(Policy = "CustomerOnly")]
    [HttpPost("milestones/{milestoneId:int}/create-order")]
    public async Task<ActionResult<ApiResponse<CashfreeOrderResponse>>> CreateMilestoneOrder(
        int milestoneId,
        [FromBody] CreatePaymentOrderRequest? request)
    {
        var customerId = await GetCurrentCustomerIdAsync();
        if (customerId == null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<CashfreeOrderResponse>.Fail("Customer account not associated."));
        }

        var result = await _paymentService.CreateMilestoneOrderAsync(milestoneId, customerId.Value, request?.ReturnUrl);
        return Ok(ApiResponse<CashfreeOrderResponse>.Ok(result, "Cashfree milestone payment session created successfully."));
    }

    /// <summary>
    /// Verify and record Cashfree payment after checkout completion
    /// </summary>
    [HttpPost("verify/{orderId}")]
    public async Task<ActionResult<ApiResponse<PaymentVerificationResponse>>> VerifyOrder(string orderId)
    {
        var result = await _paymentService.VerifyAndCompletePaymentAsync(orderId);
        if (!result.Success)
        {
            return BadRequest(ApiResponse<PaymentVerificationResponse>.Fail(result.Message));
        }

        return Ok(ApiResponse<PaymentVerificationResponse>.Ok(result, result.Message));
    }

    /// <summary>
    /// Get logged-in customer payment transaction history and receipts
    /// </summary>
    [Authorize(Policy = "CustomerOnly")]
    [HttpGet("my-payments")]
    public async Task<ActionResult<ApiResponse<List<CustomerPaymentDto>>>> GetMyPayments()
    {
        var customerId = await GetCurrentCustomerIdAsync();
        if (customerId == null)
        {
            return Ok(ApiResponse<List<CustomerPaymentDto>>.Ok(new List<CustomerPaymentDto>()));
        }

        var payments = await _paymentService.GetCustomerPaymentsAsync(customerId.Value);
        return Ok(ApiResponse<List<CustomerPaymentDto>>.Ok(payments));
    }

    /// <summary>
    /// Cashfree Webhook Callback Handler
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> CashfreeWebhook()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            var signature = Request.Headers["x-webhook-signature"].ToString();
            var timestamp = Request.Headers["x-webhook-timestamp"].ToString();

            if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(timestamp))
            {
                _logger.LogWarning("Cashfree webhook received without required signature headers.");
                return BadRequest("Missing signature headers");
            }

            var isValid = _cashfreeService.VerifyWebhookSignature(rawBody, signature, timestamp);
            if (!isValid)
            {
                _logger.LogWarning("Cashfree webhook signature mismatch.");
                return Unauthorized("Invalid webhook signature");
            }

            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var dataElem) &&
                dataElem.TryGetProperty("order", out var orderElem) &&
                orderElem.TryGetProperty("order_id", out var orderIdProp))
            {
                var orderId = orderIdProp.GetString();
                if (!string.IsNullOrWhiteSpace(orderId))
                {
                    _logger.LogInformation("Processing Cashfree webhook for order {OrderId}", orderId);
                    await _paymentService.VerifyAndCompletePaymentAsync(orderId);
                }
            }

            return Ok(new { status = "success" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Cashfree webhook");
            return StatusCode(500, "Internal error processing webhook");
        }
    }

    private async Task<int?> GetCurrentCustomerIdAsync()
    {
        var claimVal = User.FindFirstValue("customer_id");
        if (int.TryParse(claimVal, out var cid))
            return cid;

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out var uid))
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == uid);
            if (user?.CustomerId != null)
                return user.CustomerId;

            if (user != null)
            {
                var email = user.Email.Trim().ToLower();
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == email);
                if (customer != null)
                {
                    user.CustomerId = customer.Id;
                    await _context.SaveChangesAsync();
                    return customer.Id;
                }
            }
        }

        return null;
    }
}
