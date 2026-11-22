using Microsoft.AspNetCore.Mvc;
using AffordStack.Application.Common;
using AffordStack.Application.DTOs.Enquiry;
using AffordStack.Application.Interfaces;

namespace AffordStack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnquiriesController : ControllerBase
{
    private readonly IEnquiryService _enquiryService;
    private static readonly string[] AllowedExtensions = { ".pdf", ".zip", ".rar", ".docx", ".doc", ".png", ".jpg", ".jpeg", ".txt" };
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    public EnquiriesController(IEnquiryService enquiryService)
    {
        _enquiryService = enquiryService;
    }

    /// <summary>
    /// Submit a new quotation enquiry (Public, no account required)
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<EnquiryResponseDto>>> SubmitEnquiry([FromForm] CreateEnquiryRequest request, IFormFile? attachment)
    {
        (string FileName, string ContentType, byte[] Bytes)? fileData = null;

        if (attachment != null && attachment.Length > 0)
        {
            if (attachment.Length > MaxFileSizeBytes)
                return BadRequest(ApiResponse<EnquiryResponseDto>.Fail("Uploaded file exceeds 20MB limit."));

            var ext = Path.GetExtension(attachment.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return BadRequest(ApiResponse<EnquiryResponseDto>.Fail($"File format '{ext}' is not permitted for security reasons."));

            using var memoryStream = new MemoryStream();
            await attachment.CopyToAsync(memoryStream);
            fileData = (attachment.FileName, attachment.ContentType, memoryStream.ToArray());
        }

        var result = await _enquiryService.SubmitEnquiryAsync(request, fileData);
        return Ok(ApiResponse<EnquiryResponseDto>.Ok(result, "Enquiry submitted successfully."));
    }

    /// <summary>
    /// Submit enquiry via raw JSON (Public, no file attachment)
    /// </summary>
    [HttpPost("json")]
    public async Task<ActionResult<ApiResponse<EnquiryResponseDto>>> SubmitEnquiryJson([FromBody] CreateEnquiryRequest request)
    {
        var result = await _enquiryService.SubmitEnquiryAsync(request, null);
        return Ok(ApiResponse<EnquiryResponseDto>.Ok(result, "Enquiry submitted successfully."));
    }

    /// <summary>
    /// Track live enquiry lifecycle and quotation status (Public, no login required)
    /// </summary>
    [HttpPost("track")]
    public async Task<ActionResult<ApiResponse<EnquiryTrackingDto>>> TrackEnquiry([FromBody] TrackEnquiryRequest request)
    {
        var result = await _enquiryService.TrackEnquiryAsync(request);
        return Ok(ApiResponse<EnquiryTrackingDto>.Ok(result, "Enquiry tracking details retrieved."));
    }
}
