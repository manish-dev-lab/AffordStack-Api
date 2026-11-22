using Microsoft.EntityFrameworkCore;
using AffordStack.Application.DTOs.Dashboard;
using AffordStack.Application.Interfaces;
using AffordStack.Domain.Enums;
using System.Globalization;

namespace AffordStack.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;

    public DashboardService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var totalEnquiries = await _context.Enquiries.CountAsync();
        var newEnquiries = await _context.Enquiries.CountAsync(e => e.Status == EnquiryStatus.New);

        var pendingQuotes = await _context.Quotations.CountAsync(q =>
            q.Status == QuotationStatus.Draft || q.Status == QuotationStatus.Sent || q.Status == QuotationStatus.Viewed);
        var acceptedQuotes = await _context.Quotations.CountAsync(q => q.Status == QuotationStatus.Accepted);
        var rejectedQuotes = await _context.Quotations.CountAsync(q => q.Status == QuotationStatus.Rejected);

        var activeProjects = await _context.Projects.CountAsync(p =>
            p.Status == ProjectStatus.InProgress || p.Status == ProjectStatus.NotStarted);
        var completedProjects = await _context.Projects.CountAsync(p => p.Status == ProjectStatus.Completed);

        var totalRevenue = await _context.Payments
            .Where(p => p.Status == PaymentStatus.Received)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        // Pending payments on active projects
        var activeProjectsList = await _context.Projects
            .Where(p => p.Status == ProjectStatus.InProgress || p.Status == ProjectStatus.NotStarted)
            .Include(p => p.Payments)
            .ToListAsync();

        decimal pendingPayments = 0m;
        foreach (var proj in activeProjectsList)
        {
            var paid = proj.Payments.Where(p => p.Status == PaymentStatus.Received).Sum(p => p.Amount);
            var balance = proj.AgreedAmount - paid;
            if (balance > 0) pendingPayments += balance;
        }

        // Monthly trends for last 6 months
        var now = DateTime.UtcNow;
        var sixMonthsAgo = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);

        var rawMonthlyEnquiries = await _context.Enquiries
            .Where(e => e.CreatedAt >= sixMonthsAgo)
            .GroupBy(e => new { e.CreatedAt.Year, e.CreatedAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync();

        var rawMonthlyRevenue = await _context.Payments
            .Where(p => p.PaymentDate >= sixMonthsAgo && p.Status == PaymentStatus.Received)
            .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(p => p.Amount) })
            .ToListAsync();

        var monthlyEnquiries = new List<MonthlyTrendDto>();
        var monthlyRevenue = new List<MonthlyRevenueDto>();

        for (int i = 0; i < 6; i++)
        {
            var mDate = sixMonthsAgo.AddMonths(i);
            var mName = mDate.ToString("MMM yyyy", CultureInfo.InvariantCulture);

            var enqCount = rawMonthlyEnquiries
                .FirstOrDefault(x => x.Year == mDate.Year && x.Month == mDate.Month)?.Count ?? 0;
            monthlyEnquiries.Add(new MonthlyTrendDto { Month = mName, Count = enqCount });

            var revAmount = rawMonthlyRevenue
                .FirstOrDefault(x => x.Year == mDate.Year && x.Month == mDate.Month)?.Revenue ?? 0m;
            monthlyRevenue.Add(new MonthlyRevenueDto { Month = mName, Revenue = revAmount });
        }

        // Category distribution
        var rawCategoryDist = await _context.Enquiries
            .Include(e => e.ServiceCategory)
            .GroupBy(e => e.ServiceCategory != null ? e.ServiceCategory.Name : "General / Other")
            .Select(g => new CategoryDistributionDto { CategoryName = g.Key, Count = g.Count() })
            .ToListAsync();

        // Project status distribution
        var rawProjectDist = await _context.Projects
            .GroupBy(p => p.Status)
            .Select(g => new ProjectStatusDistributionDto { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        return new DashboardStatsDto
        {
            TotalEnquiries = totalEnquiries,
            NewEnquiries = newEnquiries,
            PendingQuotations = pendingQuotes,
            AcceptedQuotations = acceptedQuotes,
            RejectedQuotations = rejectedQuotes,
            ActiveProjects = activeProjects,
            CompletedProjects = completedProjects,
            TotalRevenueRecorded = totalRevenue,
            PendingPayments = pendingPayments,
            MonthlyEnquiries = monthlyEnquiries,
            MonthlyRevenue = monthlyRevenue,
            ServiceCategoryDistribution = rawCategoryDist,
            ProjectStatusDistribution = rawProjectDist
        };
    }
}
