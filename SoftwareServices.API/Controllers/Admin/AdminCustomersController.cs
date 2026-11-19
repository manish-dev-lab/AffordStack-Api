using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.Interfaces;

namespace SoftwareServices.API.Controllers.Admin;

public class CustomerListDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public int EnquiriesCount { get; set; }
    public int QuotationsCount { get; set; }
    public int ProjectsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

[Authorize]
[ApiController]
[Route("api/admin/customers")]
public class AdminCustomersController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public AdminCustomersController(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// List client records with enquiry and project counts
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CustomerListDto>>>> GetCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var query = _context.Customers
            .Include(c => c.Enquiries)
            .Include(c => c.Quotations)
            .Include(c => c.Projects)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.FullName.ToLower().Contains(s)
                || c.Email.ToLower().Contains(s)
                || c.PhoneNumber.Contains(s)
                || (c.CompanyName != null && c.CompanyName.ToLower().Contains(s)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerListDto
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                CompanyName = c.CompanyName,
                EnquiriesCount = c.Enquiries.Count,
                QuotationsCount = c.Quotations.Count,
                ProjectsCount = c.Projects.Count,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<PagedResult<CustomerListDto>>.Ok(new PagedResult<CustomerListDto>(items, total, page, pageSize)));
    }
}
