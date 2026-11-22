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
    [AllowAnonymous]
    [HttpPost("send-otp")]
    public async Task<ActionResult<ApiResponse<bool>>> SendRegistrationOtp([FromBody] SendRegistrationOtpRequest request)
    {
        var result = await _userAuthService.SendRegistrationOtpAsync(request);
        return Ok(ApiResponse<bool>.Ok(result, "Verification code sent to your email address. Please check your inbox."));
    }

    /// <summary>
    /// Register a new user account (Optional for quotation requests)
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<UserLoginResponse>>> Register([FromBody] UserRegisterRequest request)
    {
        var result = await _userAuthService.RegisterAsync(request);
        return Ok(ApiResponse<UserLoginResponse>.Ok(result, "Account created successfully. Welcome to Software Services Platform!"));
    }

    /// <summary>
    /// Sign in to existing user account
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<UserLoginResponse>>> Login([FromBody] UserLoginRequest request)
    {
        var result = await _userAuthService.LoginAsync(request);
        return Ok(ApiResponse<UserLoginResponse>.Ok(result, "Signed in successfully."));
    }

    /// <summary>
    /// Get logged-in user profile and summary metrics
    /// </summary>
    [Authorize(Policy = "CustomerOnly")]
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile()
    {
        var userType = User.FindFirstValue("user_type");
        if (userType != "customer")
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<UserProfileDto>.Fail("Access denied. Customer session required."));

        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var userId))
            return Unauthorized(ApiResponse<UserProfileDto>.Fail("Invalid session."));

        var profile = await _userAuthService.GetProfileAsync(userId);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile));
    }

    /// <summary>
    /// Update logged-in user profile details (Name, Phone, Company)
    /// </summary>
    [Authorize(Policy = "CustomerOnly")]
    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile([FromBody] UpdateUserProfileRequest request)
    {
        var userType = User.FindFirstValue("user_type");
        if (userType != "customer")
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<UserProfileDto>.Fail("Access denied. Customer session required."));

        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var userId))
            return Unauthorized(ApiResponse<UserProfileDto>.Fail("Invalid session."));

        var profile = await _userAuthService.UpdateProfileAsync(userId, request);
        return Ok(ApiResponse<UserProfileDto>.Ok(profile, "Profile updated successfully."));
    }

    /// <summary>
    /// Request OTP verification code to change account email address
    /// </summary>
    [Authorize(Policy = "CustomerOnly")]
    [HttpPost("profile/request-email-change")]
    public async Task<ActionResult<ApiResponse<bool>>> RequestEmailChange([FromBody] RequestEmailChangeOtpRequest request)
    {
        var userType = User.FindFirstValue("user_type");
        if (userType != "customer")
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<bool>.Fail("Access denied. Customer session required."));

        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var userId))
            return Unauthorized(ApiResponse<bool>.Fail("Invalid session."));

        var result = await _userAuthService.RequestEmailChangeOtpAsync(userId, request);
        return Ok(ApiResponse<bool>.Ok(result, "A 6-digit verification code has been dispatched to your new email address."));
    }

    /// <summary>
    /// Verify OTP and update account email address across database
    /// </summary>
    [Authorize(Policy = "CustomerOnly")]
    [HttpPost("profile/confirm-email-change")]
    public async Task<ActionResult<ApiResponse<UserLoginResponse>>> ConfirmEmailChange([FromBody] ConfirmEmailChangeRequest request)
    {
        var userType = User.FindFirstValue("user_type");
        if (userType != "customer")
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<UserLoginResponse>.Fail("Access denied. Customer session required."));

        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var userId))
            return Unauthorized(ApiResponse<UserLoginResponse>.Fail("Invalid session."));

        var result = await _userAuthService.ConfirmEmailChangeAsync(userId, request);
        return Ok(ApiResponse<UserLoginResponse>.Ok(result, "Your email address has been successfully updated and verified."));
    }

    /// <summary>
    /// Get past and current enquiries for the logged-in user
    /// </summary>
    [Authorize(Policy = "CustomerOnly")]
    [HttpGet("enquiries")]
    public async Task<ActionResult<ApiResponse<List<UserEnquirySummaryDto>>>> GetMyEnquiries()
    {
        var userType = User.FindFirstValue("user_type");
        if (userType != "customer")
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<List<UserEnquirySummaryDto>>.Fail("Access denied. Customer session required."));

        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var userId))
            return Unauthorized(ApiResponse<List<UserEnquirySummaryDto>>.Fail("Invalid session."));

        var enquiries = await _userAuthService.GetUserEnquiriesAsync(userId);
        return Ok(ApiResponse<List<UserEnquirySummaryDto>>.Ok(enquiries));
    }

    /// <summary>
    /// Send OTP email verification code for password reset
    /// </summary>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ForgotPassword([FromBody] UserForgotPasswordRequest request)
    {
        var result = await _userAuthService.ForgotPasswordAsync(request);
        return Ok(ApiResponse<bool>.Ok(result, "A 6-digit password reset verification code has been dispatched to your email."));
    }

    /// <summary>
    /// Reset user password using OTP verification code
    /// </summary>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword([FromBody] UserResetPasswordRequest request)
    {
        var result = await _userAuthService.ResetPasswordAsync(request);
        return Ok(ApiResponse<bool>.Ok(result, "Your password has been successfully reset. You may now sign in."));
    }

    /// <summary>
    /// Change user password
    /// </summary>
    [Authorize(Policy = "CustomerOnly")]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] UserChangePasswordRequest request)
    {
        var userType = User.FindFirstValue("user_type");
        if (userType != "customer")
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<bool>.Fail("Access denied. Customer session required."));

        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var userId))
            return Unauthorized(ApiResponse<bool>.Fail("Invalid session."));

        var result = await _userAuthService.ChangePasswordAsync(userId, request);
        return Ok(ApiResponse<bool>.Ok(result, "Password changed successfully."));
    }
}
