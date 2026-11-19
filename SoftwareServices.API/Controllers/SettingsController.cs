using Microsoft.AspNetCore.Mvc;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.Interfaces;

namespace SoftwareServices.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IContentService _contentService;

    public SettingsController(IContentService contentService)
    {
        _contentService = contentService;
    }

    /// <summary>
    /// Retrieve public website settings
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<Dictionary<string, string>>>> GetSettings()
    {
        var result = await _contentService.GetWebsiteSettingsAsync();
        return Ok(ApiResponse<Dictionary<string, string>>.Ok(result));
    }
}
