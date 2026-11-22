using AffordStack.Application.Common;
using AffordStack.Application.DTOs.Auth;
using AffordStack.Application.DTOs.Enquiry;
using AffordStack.Application.DTOs.Quotation;
using AffordStack.Application.DTOs.Project;
using AffordStack.Application.DTOs.Payment;
using AffordStack.Application.DTOs.Services;
using AffordStack.Application.DTOs.Portfolio;
using AffordStack.Application.DTOs.Blog;
using AffordStack.Application.DTOs.Testimonials;
using AffordStack.Application.DTOs.Settings;
using AffordStack.Application.DTOs.Dashboard;
using AffordStack.Domain.Enums;

namespace AffordStack.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<bool> ChangePasswordAsync(int adminUserId, ChangePasswordRequest request);
    Task<AdminUserDto> GetProfileAsync(int adminUserId);
}

public interface IEnquiryService
{
    Task<EnquiryResponseDto> SubmitEnquiryAsync(CreateEnquiryRequest request, (string FileName, string ContentType, byte[] Bytes)? attachment = null);
    Task<PagedResult<EnquiryListDto>> GetEnquiriesAsync(int page = 1, int pageSize = 10, EnquiryStatus? status = null, int? serviceCategoryId = null, string? search = null);
    Task<EnquiryDetailDto> GetEnquiryByIdAsync(int id);
    Task<bool> UpdateStatusAsync(int id, UpdateEnquiryStatusRequest request, int? adminUserId = null);
    Task<EnquiryNoteDto> AddNoteAsync(int id, AddEnquiryNoteRequest request, int? adminUserId = null);
    Task<bool> DeleteEnquiryAsync(int id);
    Task<EnquiryTrackingDto> TrackEnquiryAsync(TrackEnquiryRequest request);
}

public interface IQuotationService
{
    Task<QuotationDto> CreateQuotationAsync(CreateQuotationRequest request, string authorName = "Admin");
    Task<QuotationDto> UpdateQuotationAsync(int id, UpdateQuotationRequest request, string authorName = "Admin");
    Task<QuotationDto> GetQuotationByIdAsync(int id);
    Task<PagedResult<QuotationDto>> GetQuotationsAsync(int page = 1, int pageSize = 10, QuotationStatus? status = null, string? search = null);
    Task<bool> SendQuotationAsync(int id, string baseQuotationUrl, string authorName = "Admin");
    Task<PublicQuotationDto> GetPublicQuotationAsync(string secureToken);
    Task<bool> CustomerAcceptQuotationAsync(string secureToken, QuotationDecisionRequest request);
    Task<bool> CustomerRejectQuotationAsync(string secureToken, QuotationDecisionRequest request);
    Task<byte[]> DownloadQuotationPdfAsync(int id);
}

public interface IProjectService
{
    Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request);
    Task<ProjectDto> UpdateProjectAsync(int id, UpdateProjectRequest request);
    Task<ProjectDto> GetProjectByIdAsync(int id);
    Task<PagedResult<ProjectDto>> GetProjectsAsync(int page = 1, int pageSize = 10, ProjectStatus? status = null, string? search = null);
    Task<bool> AddMilestoneAsync(int projectId, CreateProjectMilestoneDto milestoneDto);
    Task<bool> UpdateMilestoneStatusAsync(int milestoneId, bool isCompleted);
}

public interface IPaymentService
{
    Task<PaymentDto> RecordPaymentAsync(RecordPaymentRequest request);
    Task<PagedResult<PaymentDto>> GetPaymentsAsync(int page = 1, int pageSize = 10, int? projectId = null, string? search = null);
    Task<CashfreeOrderResponse> CreateQuotationAdvanceOrderAsync(string secureToken, string? returnUrl = null);
    Task<CashfreeOrderResponse> CreateMilestoneOrderAsync(int milestoneId, int customerId, string? returnUrl = null);
    Task<PaymentVerificationResponse> VerifyAndCompletePaymentAsync(string orderId);
    Task<List<CustomerPaymentDto>> GetCustomerPaymentsAsync(int customerId);
}

public interface IDashboardService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync();
}

public interface IContentService
{
    // Services
    Task<List<ServiceCategoryDto>> GetServiceCategoriesAsync();
    Task<List<ServiceDto>> GetServicesAsync(string? categorySlug = null, bool featuredOnly = false);
    Task<ServiceDto> GetServiceBySlugAsync(string slug);
    Task<ServiceDto> CreateServiceAsync(CreateServiceRequest request);
    Task<ServiceDto> UpdateServiceAsync(int id, UpdateServiceRequest request);
    Task<bool> DeleteServiceAsync(int id);

    // Portfolio
    Task<List<PortfolioProjectDto>> GetPortfolioAsync(string? category = null, bool featuredOnly = false);
    Task<PortfolioProjectDto> GetPortfolioBySlugAsync(string slug);
    Task<PortfolioProjectDto> CreatePortfolioAsync(CreatePortfolioRequest request);
    Task<PortfolioProjectDto> UpdatePortfolioAsync(int id, UpdatePortfolioRequest request);
    Task<bool> DeletePortfolioAsync(int id);

    // Blog
    Task<List<BlogPostDto>> GetBlogPostsAsync(string? categorySlug = null, int page = 1, int pageSize = 10);
    Task<BlogPostDto> GetBlogPostBySlugAsync(string slug);
    Task<List<BlogCategoryDto>> GetBlogCategoriesAsync();
    Task<BlogPostDto> CreateBlogPostAsync(CreateBlogPostRequest request);
    Task<BlogPostDto> UpdateBlogPostAsync(int id, UpdateBlogPostRequest request);
    Task<bool> DeleteBlogPostAsync(int id);

    // Testimonials
    Task<List<TestimonialDto>> GetTestimonialsAsync(bool featuredOnly = false);
    Task<TestimonialDto> CreateTestimonialAsync(CreateTestimonialRequest request);

    // Settings
    Task<Dictionary<string, string>> GetWebsiteSettingsAsync();
    Task<bool> UpdateSettingAsync(UpdateSettingRequest request);
    Task<bool> UpdateSettingsBatchAsync(Dictionary<string, string> settings);
}
