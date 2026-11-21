using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.DTOs.User;
using SoftwareServices.Application.Interfaces;

namespace SoftwareServices.API.Controllers.Admin;

[Authorize]
[ApiController]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserAuthService _userAuthService;

    public AdminUsersController(IUserAuthService userAuthService)
    {
        _userAuthService = userAuthService;
    }

    /// <summary>
    /// List all registered customer user accounts with search and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminUserAccountDto>>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _userAuthService.GetAdminUsersAsync(page, pageSize, search);
        return Ok(ApiResponse<PagedResult<AdminUserAccountDto>>.Ok(result));
    }

    /// <summary>
    /// Get registered user account profile
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AdminUserAccountDto>>> GetUserById(int id)
    {
        var result = await _userAuthService.GetAdminUserByIdAsync(id);
        return Ok(ApiResponse<AdminUserAccountDto>.Ok(result));
    }

    /// <summary>
    /// Get all enquiries and quotations belonging to this user
    /// </summary>
    [HttpGet("{id}/enquiries")]
    public async Task<ActionResult<ApiResponse<List<UserEnquirySummaryDto>>>> GetUserEnquiries(int id)
    {
        var result = await _userAuthService.GetAdminUserEnquiriesAsync(id);
        return Ok(ApiResponse<List<UserEnquirySummaryDto>>.Ok(result));
    }

    /// <summary>
    /// Toggle user active status (activate/deactivate)
    /// </summary>
    [HttpPut("{id}/toggle-status")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleStatus(int id)
    {
        var result = await _userAuthService.ToggleUserStatusAsync(id);
        return Ok(ApiResponse<bool>.Ok(result, result ? "User account activated." : "User account deactivated."));
    }
}
