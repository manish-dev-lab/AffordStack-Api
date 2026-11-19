using Microsoft.AspNetCore.Mvc;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.DTOs.Testimonials;
using SoftwareServices.Application.Interfaces;

namespace SoftwareServices.API.Controllers;

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
