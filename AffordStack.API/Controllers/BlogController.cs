using Microsoft.AspNetCore.Mvc;
using AffordStack.Application.Common;
using AffordStack.Application.DTOs.Blog;
using AffordStack.Application.Interfaces;

namespace AffordStack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlogController : ControllerBase
{
    private readonly IContentService _contentService;

    public BlogController(IContentService contentService)
    {
        _contentService = contentService;
    }

    /// <summary>
    /// Retrieve published blog articles with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BlogPostDto>>>> GetPosts(
        [FromQuery] string? categorySlug = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _contentService.GetBlogPostsAsync(categorySlug, page, pageSize);
        return Ok(ApiResponse<List<BlogPostDto>>.Ok(result));
    }

    /// <summary>
    /// Get blog post details and increment views
    /// </summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<ApiResponse<BlogPostDto>>> GetBySlug(string slug)
    {
        var result = await _contentService.GetBlogPostBySlugAsync(slug);
        return Ok(ApiResponse<BlogPostDto>.Ok(result));
    }

    /// <summary>
    /// Get all blog categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<BlogCategoryDto>>>> GetCategories()
    {
        var result = await _contentService.GetBlogCategoriesAsync();
        return Ok(ApiResponse<List<BlogCategoryDto>>.Ok(result));
    }
}
