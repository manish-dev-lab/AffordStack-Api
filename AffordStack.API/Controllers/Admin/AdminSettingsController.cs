using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AffordStack.Application.Common;
using AffordStack.Application.DTOs.Settings;
using AffordStack.Application.DTOs.Testimonials;
using AffordStack.Application.Interfaces;

namespace AffordStack.API.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/settings")]
public class AdminSettingsController : ControllerBase
{
    private readonly IContentService _contentService;

    public AdminSettingsController(IContentService contentService)
    {
        _contentService = contentService;
    }

    /// <summary>
    /// Update website configuration setting
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateSetting([FromBody] UpdateSettingRequest request)
    {
        var result = await _contentService.UpdateSettingAsync(request);
        return Ok(ApiResponse<bool>.Ok(result, "Setting updated."));
    }

    /// <summary>
    /// Batch update website configuration settings
    /// </summary>
    [HttpPut("batch")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateSettingsBatch([FromBody] Dictionary<string, string> settings)
    {
        var result = await _contentService.UpdateSettingsBatchAsync(settings);
        return Ok(ApiResponse<bool>.Ok(result, "Settings updated successfully."));
    }

    /// <summary>
    /// Add client testimonial
    /// </summary>
    [HttpPost("testimonials")]
    public async Task<ActionResult<ApiResponse<TestimonialDto>>> CreateTestimonial([FromBody] CreateTestimonialRequest request)
    {
        var result = await _contentService.CreateTestimonialAsync(request);
        return Ok(ApiResponse<TestimonialDto>.Ok(result, "Testimonial added."));
    }

    /// <summary>
    /// Update client testimonial
    /// </summary>
    [HttpPut("testimonials/{id:int}")]
    public async Task<ActionResult<ApiResponse<TestimonialDto>>> UpdateTestimonial(int id, [FromBody] CreateTestimonialRequest request)
    {
        var result = await _contentService.UpdateTestimonialAsync(id, request);
        return Ok(ApiResponse<TestimonialDto>.Ok(result, "Testimonial updated."));
    }

    /// <summary>
    /// Delete client testimonial
    /// </summary>
    [HttpDelete("testimonials/{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTestimonial(int id)
    {
        var result = await _contentService.DeleteTestimonialAsync(id);
        return Ok(ApiResponse<bool>.Ok(result, "Testimonial deleted."));
    }
}
