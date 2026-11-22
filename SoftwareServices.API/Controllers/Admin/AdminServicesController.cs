using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.DTOs.Services;
using SoftwareServices.Application.Interfaces;

namespace SoftwareServices.API.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/services")]
public class AdminServicesController : ControllerBase
{
    private readonly IContentService _contentService;

    public AdminServicesController(IContentService contentService)
    {
        _contentService = contentService;
    }

    /// <summary>
    /// Add a new service item
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ServiceDto>>> Create([FromBody] CreateServiceRequest request)
    {
        var result = await _contentService.CreateServiceAsync(request);
        return Ok(ApiResponse<ServiceDto>.Ok(result, "Service created successfully."));
    }

    /// <summary>
    /// Update existing service
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ServiceDto>>> Update(int id, [FromBody] UpdateServiceRequest request)
    {
        var result = await _contentService.UpdateServiceAsync(id, request);
        return Ok(ApiResponse<ServiceDto>.Ok(result, "Service updated successfully."));
    }

    /// <summary>
    /// Soft delete service
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _contentService.DeleteServiceAsync(id);
        return Ok(ApiResponse<bool>.Ok(result, "Service removed."));
    }
}
