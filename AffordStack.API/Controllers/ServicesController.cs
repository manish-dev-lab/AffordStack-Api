using Microsoft.AspNetCore.Mvc;
using AffordStack.Application.Common;
using AffordStack.Application.DTOs.Services;
using AffordStack.Application.Interfaces;

namespace AffordStack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IContentService _contentService;

    public ServicesController(IContentService contentService)
    {
        _contentService = contentService;
    }

    /// <summary>
    /// Retrieve all service categories with active services
    /// </summary>
    [HttpGet("/api/service-categories")]
    public async Task<ActionResult<ApiResponse<List<ServiceCategoryDto>>>> GetCategories()
    {
        var result = await _contentService.GetServiceCategoriesAsync();
        return Ok(ApiResponse<List<ServiceCategoryDto>>.Ok(result));
    }

    /// <summary>
    /// List services filtered by category slug or featured status
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ServiceDto>>>> GetServices([FromQuery] string? categorySlug = null, [FromQuery] bool featuredOnly = false)
    {
        var result = await _contentService.GetServicesAsync(categorySlug, featuredOnly);
        return Ok(ApiResponse<List<ServiceDto>>.Ok(result));
    }

    /// <summary>
    /// Get individual service details by slug
    /// </summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<ApiResponse<ServiceDto>>> GetBySlug(string slug)
    {
        var result = await _contentService.GetServiceBySlugAsync(slug);
        return Ok(ApiResponse<ServiceDto>.Ok(result));
    }
}
