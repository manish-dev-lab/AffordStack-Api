using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AffordStack.Application.Common;
using AffordStack.Application.DTOs.Portfolio;
using AffordStack.Application.Interfaces;

namespace AffordStack.API.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/portfolio")]
public class AdminPortfolioController : ControllerBase
{
    private readonly IContentService _contentService;

    public AdminPortfolioController(IContentService contentService)
    {
        _contentService = contentService;
    }

    /// <summary>
    /// Create portfolio project showcase
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<PortfolioProjectDto>>> Create([FromBody] CreatePortfolioRequest request)
    {
        var result = await _contentService.CreatePortfolioAsync(request);
        return Ok(ApiResponse<PortfolioProjectDto>.Ok(result, "Portfolio project added."));
    }

    /// <summary>
    /// Update portfolio project
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<PortfolioProjectDto>>> Update(int id, [FromBody] UpdatePortfolioRequest request)
    {
        var result = await _contentService.UpdatePortfolioAsync(id, request);
        return Ok(ApiResponse<PortfolioProjectDto>.Ok(result, "Portfolio project updated."));
    }

    /// <summary>
    /// Delete portfolio project
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _contentService.DeletePortfolioAsync(id);
        return Ok(ApiResponse<bool>.Ok(result, "Portfolio project deleted."));
    }
}
