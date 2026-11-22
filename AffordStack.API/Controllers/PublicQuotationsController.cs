using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AffordStack.Application.Common;
using AffordStack.Application.DTOs.Quotation;
using AffordStack.Application.Interfaces;

namespace AffordStack.API.Controllers;

[ApiController]
[Route("api/public/quotations")]
public class PublicQuotationsController : ControllerBase
{
    private readonly IQuotationService _quotationService;
    private readonly IApplicationDbContext _context;

    public PublicQuotationsController(IQuotationService quotationService, IApplicationDbContext context)
    {
        _quotationService = quotationService;
        _context = context;
    }

    /// <summary>
    /// View quotation proposal using unique secure link token (No login required)
    /// </summary>
    [HttpGet("{secureToken}")]
    public async Task<ActionResult<ApiResponse<PublicQuotationDto>>> GetPublicQuotation(string secureToken)
    {
        var result = await _quotationService.GetPublicQuotationAsync(secureToken);
        return Ok(ApiResponse<PublicQuotationDto>.Ok(result));
    }

    /// <summary>
    /// Customer accepts quotation proposal
    /// </summary>
    [HttpPost("{secureToken}/accept")]
    public async Task<ActionResult<ApiResponse<bool>>> AcceptQuotation(string secureToken, [FromBody] QuotationDecisionRequest request)
    {
        var result = await _quotationService.CustomerAcceptQuotationAsync(secureToken, request);
        return Ok(ApiResponse<bool>.Ok(result, "Quotation accepted successfully! Our project team has been notified."));
    }

    /// <summary>
    /// Customer rejects quotation proposal
    /// </summary>
    [HttpPost("{secureToken}/reject")]
    public async Task<ActionResult<ApiResponse<bool>>> RejectQuotation(string secureToken, [FromBody] QuotationDecisionRequest request)
    {
        var result = await _quotationService.CustomerRejectQuotationAsync(secureToken, request);
        return Ok(ApiResponse<bool>.Ok(result, "Your feedback has been recorded. Thank you."));
    }

    /// <summary>
    /// Download PDF of the quotation proposal via secure token
    /// </summary>
    [HttpGet("{secureToken}/pdf")]
    public async Task<IActionResult> DownloadPdf(string secureToken)
    {
        var quote = await _context.Quotations.AsNoTracking().FirstOrDefaultAsync(q => q.SecureToken == secureToken);

        if (quote == null) return NotFound("Quotation not found.");

        var pdfBytes = await _quotationService.DownloadQuotationPdfAsync(quote.Id);
        return File(pdfBytes, "application/pdf", $"Quotation_{quote.QuotationNumber}.pdf");
    }
}
