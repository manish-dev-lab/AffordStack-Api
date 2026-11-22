using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AffordStack.Application.Common;
using AffordStack.Application.DTOs.Enquiry;
using AffordStack.Application.Interfaces;
using AffordStack.Domain.Enums;
using System.Security.Claims;

namespace AffordStack.API.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/enquiries")]
public class AdminEnquiriesController : ControllerBase
{
    private readonly IEnquiryService _enquiryService;

    public AdminEnquiriesController(IEnquiryService enquiryService)
    {
        _enquiryService = enquiryService;
    }

    /// <summary>
    /// List enquiries with pagination, filters and search
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<EnquiryListDto>>>> GetEnquiries(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] EnquiryStatus? status = null,
        [FromQuery] int? serviceCategoryId = null,
        [FromQuery] string? search = null)
    {
        var result = await _enquiryService.GetEnquiriesAsync(page, pageSize, status, serviceCategoryId, search);
        return Ok(ApiResponse<PagedResult<EnquiryListDto>>.Ok(result));
    }

    /// <summary>
    /// Get full enquiry details including notes and attachments
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<EnquiryDetailDto>>> GetById(int id)
    {
        var result = await _enquiryService.GetEnquiryByIdAsync(id);
        return Ok(ApiResponse<EnquiryDetailDto>.Ok(result));
    }

    /// <summary>
    /// Update enquiry workflow status
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStatus(int id, [FromBody] UpdateEnquiryStatusRequest request)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        var result = await _enquiryService.UpdateStatusAsync(id, request, adminId > 0 ? adminId : null);
        return Ok(ApiResponse<bool>.Ok(result, "Enquiry status updated successfully."));
    }

    /// <summary>
    /// Append an internal administration note to the enquiry
    /// </summary>
    [HttpPost("{id}/notes")]
    public async Task<ActionResult<ApiResponse<EnquiryNoteDto>>> AddNote(int id, [FromBody] AddEnquiryNoteRequest request)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId);
        var result = await _enquiryService.AddNoteAsync(id, request, adminId > 0 ? adminId : null);
        return Ok(ApiResponse<EnquiryNoteDto>.Ok(result, "Note added successfully."));
    }

    /// <summary>
    /// Soft delete enquiry
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _enquiryService.DeleteEnquiryAsync(id);
        return Ok(ApiResponse<bool>.Ok(result, "Enquiry removed."));
    }
}
