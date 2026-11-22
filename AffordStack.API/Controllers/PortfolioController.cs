using Microsoft.AspNetCore.Mvc;
using AffordStack.Application.Common;
using AffordStack.Application.DTOs.Portfolio;
using AffordStack.Application.Interfaces;

namespace AffordStack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortfolioController : ControllerBase
{
    private readonly IContentService _contentService;

    public PortfolioController(IContentService contentService)
    {
        _contentService = contentService;
    }

    /// <summary>
    /// Retrieve portfolio items optionally filtered by category
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PortfolioProjectDto>>>> GetPortfolio([FromQuery] string? category = null, [FromQuery] bool featuredOnly = false)
    {
        var result = await _contentService.GetPortfolioAsync(category, featuredOnly);
        return Ok(ApiResponse<List<PortfolioProjectDto>>.Ok(result));
    }

    /// <summary>
    /// Get detailed portfolio project by slug
    /// </summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<ApiResponse<PortfolioProjectDto>>> GetBySlug(string slug)
    {
        var result = await _contentService.GetPortfolioBySlugAsync(slug);
        return Ok(ApiResponse<PortfolioProjectDto>.Ok(result));
    }
}
