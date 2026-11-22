using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.DTOs.Configuration;
using SoftwareServices.Application.Interfaces;

namespace SoftwareServices.API.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/configurations")]
public class AdminConfigurationsController : ControllerBase
{
    private readonly IConfigurationService _configurationService;

    public AdminConfigurationsController(IConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <summary>
    /// Get all system configurations stored in the database
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AppConfigItemDto>>>> GetAll()
    {
        var configs = await _configurationService.GetAllConfigurationsAsync();
        return Ok(ApiResponse<List<AppConfigItemDto>>.Ok(configs));
    }

    /// <summary>
    /// Get token configuration (timeout, expiration)
    /// </summary>
    [HttpGet("token")]
    public async Task<ActionResult<ApiResponse<TokenConfigDto>>> GetTokenConfig()
    {
        var config = await _configurationService.GetTokenConfigAsync();
        return Ok(ApiResponse<TokenConfigDto>.Ok(config));
    }

    /// <summary>
    /// Get email notifications and SMTP configuration
    /// </summary>
    [HttpGet("email")]
    public async Task<ActionResult<ApiResponse<EmailConfigDto>>> GetEmailConfig()
    {
        var config = await _configurationService.GetEmailConfigAsync();
        return Ok(ApiResponse<EmailConfigDto>.Ok(config));
    }

    /// <summary>
    /// Get website operational configuration
    /// </summary>
    [HttpGet("website")]
    public async Task<ActionResult<ApiResponse<WebsiteConfigDto>>> GetWebsiteConfig()
    {
        var config = await _configurationService.GetWebsiteConfigAsync();
        return Ok(ApiResponse<WebsiteConfigDto>.Ok(config));
    }

    /// <summary>
    /// Update a specific configuration key
    /// </summary>
    [HttpPut("{key}")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateConfig(string key, [FromBody] UpdateConfigValueRequest request)
    {
        var result = await _configurationService.UpdateConfigurationAsync(key, request.Value, request.Description);
        return Ok(ApiResponse<bool>.Ok(result, $"Configuration '{key}' updated successfully."));
    }

    /// <summary>
    /// Batch update configurations
    /// </summary>
    [HttpPut("batch")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateBatch([FromBody] Dictionary<string, string> configs)
    {
        var result = await _configurationService.UpdateConfigurationsBatchAsync(configs);
        return Ok(ApiResponse<bool>.Ok(result, "Configurations updated successfully."));
    }
}
