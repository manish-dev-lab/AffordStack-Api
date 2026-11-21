using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.DTOs.Quotation;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Domain.Enums;
using System.Security.Claims;

namespace SoftwareServices.API.Controllers.Admin;

[Authorize]
[ApiController]
[Route("api/admin/quotations")]
public class AdminQuotationsController : ControllerBase
{
    private readonly IQuotationService _quotationService;

    public AdminQuotationsController(IQuotationService quotationService)
    {
        _quotationService = quotationService;
    }

    /// <summary>
    /// List all quotations with filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<QuotationDto>>>> GetQuotations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] QuotationStatus? status = null,
        [FromQuery] string? search = null)
    {
        var result = await _quotationService.GetQuotationsAsync(page, pageSize, status, search);
        return Ok(ApiResponse<PagedResult<QuotationDto>>.Ok(result));
    }

    /// <summary>
    /// Get full quotation record by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> GetById(int id)
    {
        var result = await _quotationService.GetQuotationByIdAsync(id);
        return Ok(ApiResponse<QuotationDto>.Ok(result));
    }

    /// <summary>
    /// Create quotation proposal for an enquiry
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Create([FromBody] CreateQuotationRequest request)
    {
        var author = User.FindFirstValue(ClaimTypes.GivenName) ?? User.FindFirstValue(ClaimTypes.Name) ?? "Admin";
        var result = await _quotationService.CreateQuotationAsync(request, author);
        return Ok(ApiResponse<QuotationDto>.Ok(result, "Quotation created successfully."));
    }

    /// <summary>
    /// Update quotation details
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<QuotationDto>>> Update(int id, [FromBody] UpdateQuotationRequest request)
    {
        var author = User.FindFirstValue(ClaimTypes.GivenName) ?? User.FindFirstValue(ClaimTypes.Name) ?? "Admin";
        var result = await _quotationService.UpdateQuotationAsync(id, request, author);
        return Ok(ApiResponse<QuotationDto>.Ok(result, "Quotation updated successfully."));
    }

    /// <summary>
    /// Send official quotation to customer via email with PDF attachment
    /// </summary>
    [HttpPost("{id}/send")]
    public async Task<ActionResult<ApiResponse<bool>>> Send(int id)
    {
        var author = User.FindFirstValue(ClaimTypes.GivenName) ?? User.FindFirstValue(ClaimTypes.Name) ?? "Admin";
        var origin = $"{Request.Scheme}://{Request.Host}";
        var result = await _quotationService.SendQuotationAsync(id, origin, author);
        return Ok(ApiResponse<bool>.Ok(result, "Quotation PDF generated and dispatched via email."));
    }

    /// <summary>
    /// Download PDF quotation document
    /// </summary>
    [HttpGet("{id}/pdf")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var quote = await _quotationService.GetQuotationByIdAsync(id);
        var pdfBytes = await _quotationService.DownloadQuotationPdfAsync(id);
        return File(pdfBytes, "application/pdf", $"Quotation_{quote.QuotationNumber}.pdf");
    }
}
