using Microsoft.AspNetCore.Mvc;
using AffordStack.Application.Common;
using AffordStack.Application.DTOs.Testimonials;
using AffordStack.Application.Interfaces;

namespace AffordStack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestimonialsController : ControllerBase
{
    private readonly IContentService _contentService;

    public TestimonialsController(IContentService contentService)
    {
        _contentService = contentService;
    }

    /// <summary>
    /// Retrieve testimonials
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TestimonialDto>>>> GetTestimonials([FromQuery] bool featuredOnly = false)
    {
        var result = await _contentService.GetTestimonialsAsync(featuredOnly);
        return Ok(ApiResponse<List<TestimonialDto>>.Ok(result));
    }
}
