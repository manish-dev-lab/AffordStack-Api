using Microsoft.EntityFrameworkCore;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.Common.Exceptions;
using SoftwareServices.Application.DTOs.Project;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Domain.Entities;
using SoftwareServices.Domain.Enums;

namespace SoftwareServices.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IApplicationDbContext _context;

    public ProjectService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request)
    {
        var customer = await _context.Customers.FindAsync(request.CustomerId);
        if (customer == null)
            throw new NotFoundException($"Customer #{request.CustomerId} not found.");

        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var countToday = await _context.Projects.CountAsync(p => p.ProjectNumber.StartsWith($"PRJ-{datePrefix}"));
        var projNumber = $"PRJ-{datePrefix}-{(countToday + 1):D4}";

        var project = new Project
        {
            ProjectNumber = projNumber,
            QuotationId = request.QuotationId,
            CustomerId = customer.Id,
            Customer = customer,
            ProjectTitle = request.ProjectTitle.Trim(),
            Description = request.Description.Trim(),
            StartDate = request.StartDate,
            ExpectedCompletionDate = request.ExpectedCompletionDate,
            AgreedAmount = request.AgreedAmount,
            Status = ProjectStatus.InProgress,
            ProgressPercentage = 0,
            AssignedDeveloper = request.AssignedDeveloper?.Trim(),
            InternalNotes = request.InternalNotes?.Trim()
        };

        foreach (var m in request.Milestones)
        {
            project.Milestones.Add(new ProjectMilestone
            {
                Title = m.Title.Trim(),
                Description = m.Description.Trim(),
                DueDate = m.DueDate,
                Amount = m.Amount
            });
        }

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return MapToDto(project, 0);
    }

    public async Task<ProjectDto> UpdateProjectAsync(int id, UpdateProjectRequest request)
    {
        var project = await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Milestones)
            .Include(p => p.Payments)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
            throw new NotFoundException($"Project #{id} not found.");

        project.ProjectTitle = request.ProjectTitle.Trim();
        project.Description = request.Description.Trim();
        project.ExpectedCompletionDate = request.ExpectedCompletionDate;
        project.AgreedAmount = request.AgreedAmount;
        project.Status = request.Status;
        project.ProgressPercentage = Math.Clamp(request.ProgressPercentage, 0, 100);
        project.AssignedDeveloper = request.AssignedDeveloper?.Trim();
        project.InternalNotes = request.InternalNotes?.Trim();
        project.UpdatedAt = DateTime.UtcNow;

        if (request.Status == ProjectStatus.Completed && project.ActualCompletionDate == null)
        {
            project.ActualCompletionDate = DateTime.UtcNow;
            project.ProgressPercentage = 100;
        }

        await _context.SaveChangesAsync();
        var totalPaid = project.Payments.Where(p => p.Status == PaymentStatus.Received).Sum(p => p.Amount);
        return MapToDto(project, totalPaid);
    }

    public async Task<ProjectDto> GetProjectByIdAsync(int id)
    {
        var project = await _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Quotation)
            .Include(p => p.Milestones)
            .Include(p => p.Payments)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
            throw new NotFoundException($"Project #{id} not found.");

        var totalPaid = project.Payments.Where(p => p.Status == PaymentStatus.Received).Sum(p => p.Amount);
        return MapToDto(project, totalPaid);
    }

    public async Task<PagedResult<ProjectDto>> GetProjectsAsync(int page = 1, int pageSize = 10, ProjectStatus? status = null, string? search = null)
    {
        var query = _context.Projects
            .Include(p => p.Customer)
            .Include(p => p.Quotation)
            .Include(p => p.Milestones)
            .Include(p => p.Payments)
            .AsNoTracking();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.ProjectNumber.ToLower().Contains(s)
                || p.ProjectTitle.ToLower().Contains(s)
                || p.Customer!.FullName.ToLower().Contains(s));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(p =>
        {
            var paid = p.Payments.Where(pay => pay.Status == PaymentStatus.Received).Sum(pay => pay.Amount);
            return MapToDto(p, paid);
        }).ToList();

        return new PagedResult<ProjectDto>(dtos, total, page, pageSize);
    }

    public async Task<bool> AddMilestoneAsync(int projectId, CreateProjectMilestoneDto milestoneDto)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null)
            throw new NotFoundException($"Project #{projectId} not found.");

        _context.ProjectMilestones.Add(new ProjectMilestone
        {
            ProjectId = projectId,
            Title = milestoneDto.Title.Trim(),
            Description = milestoneDto.Description.Trim(),
            DueDate = milestoneDto.DueDate,
            Amount = milestoneDto.Amount
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateMilestoneStatusAsync(int milestoneId, bool isCompleted)
    {
        var milestone = await _context.ProjectMilestones.FindAsync(milestoneId);
        if (milestone == null)
            throw new NotFoundException($"Milestone #{milestoneId} not found.");

        milestone.IsCompleted = isCompleted;
        milestone.CompletedAt = isCompleted ? DateTime.UtcNow : null;
        milestone.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private static ProjectDto MapToDto(Project p, decimal totalPaid)
    {
        return new ProjectDto
        {
            Id = p.Id,
            ProjectNumber = p.ProjectNumber,
            QuotationId = p.QuotationId,
            QuotationNumber = p.Quotation?.QuotationNumber,
            CustomerId = p.CustomerId,
            CustomerName = p.Customer?.FullName ?? "Unknown",
            CustomerEmail = p.Customer?.Email ?? "",
            CustomerPhone = p.Customer?.PhoneNumber ?? "",
            ProjectTitle = p.ProjectTitle,
            Description = p.Description,
            StartDate = p.StartDate,
            ExpectedCompletionDate = p.ExpectedCompletionDate,
            ActualCompletionDate = p.ActualCompletionDate,
            AgreedAmount = p.AgreedAmount,
            TotalPaid = totalPaid,
            Status = p.Status,
            ProgressPercentage = p.ProgressPercentage,
            AssignedDeveloper = p.AssignedDeveloper,
            InternalNotes = p.InternalNotes,
            CreatedAt = p.CreatedAt,
            Milestones = p.Milestones.Select(m => new ProjectMilestoneDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                DueDate = m.DueDate,
                IsCompleted = m.IsCompleted,
                CompletedAt = m.CompletedAt,
                Amount = m.Amount
            }).ToList()
        };
    }
}
