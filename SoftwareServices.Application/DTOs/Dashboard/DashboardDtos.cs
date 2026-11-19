namespace SoftwareServices.Application.DTOs.Dashboard;

public class DashboardStatsDto
{
    public int TotalEnquiries { get; set; }
    public int NewEnquiries { get; set; }
    public int PendingQuotations { get; set; }
    public int AcceptedQuotations { get; set; }
    public int RejectedQuotations { get; set; }
    public int ActiveProjects { get; set; }
    public int CompletedProjects { get; set; }
    public decimal TotalRevenueRecorded { get; set; }
    public decimal PendingPayments { get; set; }

    public List<MonthlyTrendDto> MonthlyEnquiries { get; set; } = new();
    public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
    public List<CategoryDistributionDto> ServiceCategoryDistribution { get; set; } = new();
    public List<ProjectStatusDistributionDto> ProjectStatusDistribution { get; set; } = new();
}

public class MonthlyTrendDto
{
    public string Month { get; set; } = string.Empty; // e.g. "Jan", "Feb"
    public int Count { get; set; }
}

public class MonthlyRevenueDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class CategoryDistributionDto
{
    public string CategoryName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ProjectStatusDistributionDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}
