using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.DTOs.Project;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Domain.Enums;

namespace SoftwareServices.API.Controllers.Admin;

[Authorize]
[ApiController]
[Route("api/admin/projects")]
public class AdminProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public AdminProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// List ongoing and completed client projects
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProjectDto>>>> GetProjects(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ProjectStatus? status = null,
        [FromQuery] string? search = null)
    {
        var result = await _projectService.GetProjectsAsync(page, pageSize, status, search);
        return Ok(ApiResponse<PagedResult<ProjectDto>>.Ok(result));
    }

    /// <summary>
    /// Get project details including milestones and payment history
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> GetById(int id)
    {
        var result = await _projectService.GetProjectByIdAsync(id);
        return Ok(ApiResponse<ProjectDto>.Ok(result));
    }

    /// <summary>
    /// Create new project
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> Create([FromBody] CreateProjectRequest request)
    {
        var result = await _projectService.CreateProjectAsync(request);
        return Ok(ApiResponse<ProjectDto>.Ok(result, "Project created successfully."));
    }

    /// <summary>
    /// Update project details and progress
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> Update(int id, [FromBody] UpdateProjectRequest request)
    {
        var result = await _projectService.UpdateProjectAsync(id, request);
        return Ok(ApiResponse<ProjectDto>.Ok(result, "Project updated successfully."));
    }

    /// <summary>
    /// Add milestone to project
    /// </summary>
    [HttpPost("{id}/milestones")]
    public async Task<ActionResult<ApiResponse<bool>>> AddMilestone(int id, [FromBody] CreateProjectMilestoneDto request)
    {
        var result = await _projectService.AddMilestoneAsync(id, request);
        return Ok(ApiResponse<bool>.Ok(result, "Milestone added."));
    }

    /// <summary>
    /// Update milestone completion status
    /// </summary>
    [HttpPut("milestones/{milestoneId}/status")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateMilestoneStatus(int milestoneId, [FromBody] bool isCompleted)
    {
        var result = await _projectService.UpdateMilestoneStatusAsync(milestoneId, isCompleted);
        return Ok(ApiResponse<bool>.Ok(result, "Milestone status updated."));
    }
}
