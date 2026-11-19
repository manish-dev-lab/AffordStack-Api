using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.DTOs.Auth;
using SoftwareServices.Application.Interfaces;
using System.Security.Claims;

namespace SoftwareServices.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticate administrator and receive JWT token
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(ApiResponse<LoginResponse>.Ok(response, "Login successful."));
    }

    /// <summary>
    /// Change administrator password
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized(ApiResponse<bool>.Fail("Unauthorized access."));

        await _authService.ChangePasswordAsync(userId, request);
        return Ok(ApiResponse<bool>.Ok(true, "Password changed successfully."));
    }

    /// <summary>
    /// Get logged in administrator profile
    /// </summary>
    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<AdminUserDto>>> GetProfile()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return Unauthorized(ApiResponse<AdminUserDto>.Fail("Unauthorized access."));

        var profile = await _authService.GetProfileAsync(userId);
        return Ok(ApiResponse<AdminUserDto>.Ok(profile));
    }
}
