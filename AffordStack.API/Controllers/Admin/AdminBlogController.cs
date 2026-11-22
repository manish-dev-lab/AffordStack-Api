using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AffordStack.Application.Common;
using AffordStack.Application.DTOs.Blog;
using AffordStack.Application.Interfaces;

namespace AffordStack.API.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/blog")]
public class AdminBlogController : ControllerBase
{
    private readonly IContentService _contentService;

    public AdminBlogController(IContentService contentService)
    {
        _contentService = contentService;
    }

    /// <summary>
    /// Publish new technical article
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<BlogPostDto>>> Create([FromBody] CreateBlogPostRequest request)
    {
        var result = await _contentService.CreateBlogPostAsync(request);
        return Ok(ApiResponse<BlogPostDto>.Ok(result, "Article published."));
    }

    /// <summary>
    /// Update technical article
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<BlogPostDto>>> Update(int id, [FromBody] UpdateBlogPostRequest request)
    {
        var result = await _contentService.UpdateBlogPostAsync(id, request);
        return Ok(ApiResponse<BlogPostDto>.Ok(result, "Article updated."));
    }

    /// <summary>
    /// Delete article
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
    {
        var result = await _contentService.DeleteBlogPostAsync(id);
        return Ok(ApiResponse<bool>.Ok(result, "Article deleted."));
    }
}
