using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.DTOs.User;
using SoftwareServices.Application.Interfaces;
using System.Security.Claims;

namespace SoftwareServices.API.Controllers;

[ApiController]
[Route("api/user/auth")]
public class UserAuthController : ControllerBase
{
    private readonly IUserAuthService _userAuthService;

    public UserAuthController(IUserAuthService userAuthService)
    {
        _userAuthService = userAuthService;
    }

    /// <summary>
    /// Send OTP email verification code for new user account registration
    /// </summary>
    [HttpPost("send-otp")]
    public async Task<ActionResult<ApiResponse<bool>>> SendRegistrationOtp([FromBody] SendRegistrationOtpRequest request)
    {
        var result = await _userAuthService.SendRegistrationOtpAsync(request);
        return Ok(ApiResponse<bool>.Ok(result, "Verification code sent to your email address. Please check your inbox."));
    }

    /// <summary>
    /// Register a new user account (Optional for quotation requests)
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<UserLoginResponse>>> Register([FromBody] UserRegisterRequest request)
    {
        var result = await _userAuthService.RegisterAsync(request);
        return Ok(ApiResponse<UserLoginResponse>.Ok(result, "Account created successfully. Welcome to Software Services Platform!"));
    }

    /// <summary>
    /// Sign in to existing user account
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<UserLoginResponse>>> Login([FromBody] UserLoginRequest request)
    {
        var result = await _userAuthService.LoginAsync(request);
        return Ok(ApiResponse<UserLoginResponse>.Ok(result, "Signed in successfully."));
    }

    /// <summary>
    /// Get logged-in user profile and summary metrics
    /// </summary>
    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var userId))
            return Unauthorized(ApiResponse<UserProfileDto>.Fail("Invalid session."));

        var profile = await _userAuthService.GetProfileAsync(userId);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile));
    }

    /// <summary>
    /// Get past and current enquiries for the logged-in user
    /// </summary>
    [Authorize]
    [HttpGet("enquiries")]
    public async Task<ActionResult<ApiResponse<List<UserEnquirySummaryDto>>>> GetMyEnquiries()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var userId))
            return Unauthorized(ApiResponse<List<UserEnquirySummaryDto>>.Fail("Invalid session."));

        var enquiries = await _userAuthService.GetUserEnquiriesAsync(userId);
        return Ok(ApiResponse<List<UserEnquirySummaryDto>>.Ok(enquiries));
    }

    /// <summary>
    /// Change user password
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] UserChangePasswordRequest request)
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var userId))
            return Unauthorized(ApiResponse<bool>.Fail("Invalid session."));

        var result = await _userAuthService.ChangePasswordAsync(userId, request);
        return Ok(ApiResponse<bool>.Ok(result, "Password changed successfully."));
    }
}
