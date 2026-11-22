using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.DTOs.Payment;
using SoftwareServices.Application.Interfaces;

namespace SoftwareServices.API.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/payments")]
public class AdminPaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public AdminPaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// List client payments
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PaymentDto>>>> GetPayments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? projectId = null,
        [FromQuery] string? search = null)
    {
        var result = await _paymentService.GetPaymentsAsync(page, pageSize, projectId, search);
        return Ok(ApiResponse<PagedResult<PaymentDto>>.Ok(result));
    }

    /// <summary>
    /// Record a payment (Advance, Milestone, Final)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> RecordPayment([FromBody] RecordPaymentRequest request)
    {
        var result = await _paymentService.RecordPaymentAsync(request);
        return Ok(ApiResponse<PaymentDto>.Ok(result, "Payment recorded successfully."));
    }
}
