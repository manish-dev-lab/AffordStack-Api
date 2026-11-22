using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AffordStack.Application.Common;
using AffordStack.Application.DTOs.Dashboard;
using AffordStack.Application.Interfaces;

namespace AffordStack.API.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[Route("api/admin/dashboard")]
public class AdminDashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public AdminDashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Get aggregated metrics and monthly trend charts for dashboard
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats()
    {
        var result = await _dashboardService.GetDashboardStatsAsync();
        return Ok(ApiResponse<DashboardStatsDto>.Ok(result));
    }
}
