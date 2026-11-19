using Microsoft.EntityFrameworkCore;
using SoftwareServices.Application.Common.Exceptions;
using SoftwareServices.Application.DTOs.Blog;
using SoftwareServices.Application.DTOs.Portfolio;
using SoftwareServices.Application.DTOs.Services;
using SoftwareServices.Application.DTOs.Settings;
using SoftwareServices.Application.DTOs.Testimonials;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Domain.Entities;
using System.Text.Json;

namespace SoftwareServices.Application.Services;

public class ContentService : IContentService
{
    private readonly IApplicationDbContext _context;

    public ContentService(IApplicationDbContext context)
    {
        _context = context;
    }

    #region Services & Categories
    public async Task<List<ServiceCategoryDto>> GetServiceCategoriesAsync()
    {
        var categories = await _context.ServiceCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Include(c => c.Services.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder))
            .AsNoTracking()
            .ToListAsync();

        return categories.Select(c => new ServiceCategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            Description = c.Description,
            IconName = c.IconName,
            DisplayOrder = c.DisplayOrder,
            IsActive = c.IsActive,
            Services = c.Services.Select(MapToServiceDto).ToList()
        }).ToList();
    }

    public async Task<List<ServiceDto>> GetServicesAsync(string? categorySlug = null, bool featuredOnly = false)
    {
        var query = _context.Services
            .Include(s => s.ServiceCategory)
            .Where(s => s.IsActive)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(categorySlug))
            query = query.Where(s => s.ServiceCategory != null && s.ServiceCategory.Slug == categorySlug.ToLower());

        if (featuredOnly)
            query = query.Where(s => s.IsFeatured);

        var list = await query.OrderBy(s => s.DisplayOrder).ToListAsync();
        return list.Select(MapToServiceDto).ToList();
    }

    public async Task<ServiceDto> GetServiceBySlugAsync(string slug)
    {
        var service = await _context.Services
            .Include(s => s.ServiceCategory)
            .FirstOrDefaultAsync(s => s.Slug.ToLower() == slug.ToLower());

        if (service == null)
            throw new NotFoundException($"Service '{slug}' not found.");

        return MapToServiceDto(service);
    }

    public async Task<ServiceDto> CreateServiceAsync(CreateServiceRequest request)
    {
        var category = await _context.ServiceCategories.FindAsync(request.ServiceCategoryId);
        if (category == null)
            throw new NotFoundException("Service category not found.");

        var service = new ServiceItem
        {
            ServiceCategoryId = category.Id,
            Title = request.Title.Trim(),
            Slug = string.IsNullOrWhiteSpace(request.Slug) ? GenerateSlug(request.Title) : request.Slug.Trim().ToLower(),
            ShortDescription = request.ShortDescription.Trim(),
            FullDescription = request.FullDescription.Trim(),
            FeaturesJson = JsonSerializer.Serialize(request.Features ?? new()),
            TechnologiesJson = JsonSerializer.Serialize(request.Technologies ?? new()),
            BenefitsJson = JsonSerializer.Serialize(request.Benefits ?? new()),
            IconName = request.IconName,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            IsFeatured = request.IsFeatured
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        service.ServiceCategory = category;
        return MapToServiceDto(service);
    }

    public async Task<ServiceDto> UpdateServiceAsync(int id, UpdateServiceRequest request)
    {
        var service = await _context.Services
            .Include(s => s.ServiceCategory)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (service == null)
            throw new NotFoundException($"Service #{id} not found.");

        service.ServiceCategoryId = request.ServiceCategoryId;
        service.Title = request.Title.Trim();
        service.Slug = string.IsNullOrWhiteSpace(request.Slug) ? GenerateSlug(request.Title) : request.Slug.Trim().ToLower();
        service.ShortDescription = request.ShortDescription.Trim();
        service.FullDescription = request.FullDescription.Trim();
        service.FeaturesJson = JsonSerializer.Serialize(request.Features ?? new());
        service.TechnologiesJson = JsonSerializer.Serialize(request.Technologies ?? new());
        service.BenefitsJson = JsonSerializer.Serialize(request.Benefits ?? new());
        service.IconName = request.IconName;
        service.DisplayOrder = request.DisplayOrder;
        service.IsActive = request.IsActive;
        service.IsFeatured = request.IsFeatured;
        service.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToServiceDto(service);
    }

    public async Task<bool> DeleteServiceAsync(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
            throw new NotFoundException($"Service #{id} not found.");

        service.IsDeleted = true;
        service.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
    #endregion

    #region Portfolio
    public async Task<List<PortfolioProjectDto>> GetPortfolioAsync(string? category = null, bool featuredOnly = false)
    {
        var query = _context.PortfolioProjects
            .Where(p => p.IsActive)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            query = query.Where(p => p.Category.ToLower() == category.ToLower());

        if (featuredOnly)
            query = query.Where(p => p.IsFeatured);

        var list = await query.OrderBy(p => p.DisplayOrder).ThenByDescending(p => p.CreatedAt).ToListAsync();
        return list.Select(MapToPortfolioDto).ToList();
    }

    public async Task<PortfolioProjectDto> GetPortfolioBySlugAsync(string slug)
    {
        var proj = await _context.PortfolioProjects
            .FirstOrDefaultAsync(p => p.Slug.ToLower() == slug.ToLower());

        if (proj == null)
            throw new NotFoundException($"Portfolio project '{slug}' not found.");

        return MapToPortfolioDto(proj);
    }

    public async Task<PortfolioProjectDto> CreatePortfolioAsync(CreatePortfolioRequest request)
    {
        var proj = new PortfolioProject
        {
            Title = request.Title.Trim(),
            Slug = string.IsNullOrWhiteSpace(request.Slug) ? GenerateSlug(request.Title) : request.Slug.Trim().ToLower(),
            ShortDescription = request.ShortDescription.Trim(),
            DetailedDescription = request.DetailedDescription.Trim(),
            Category = request.Category.Trim(),
            ThumbnailUrl = request.ThumbnailUrl,
            LiveDemoUrl = request.LiveDemoUrl,
            GitHubUrl = request.GitHubUrl,
            TechnologiesJson = JsonSerializer.Serialize(request.Technologies ?? new()),
            FeaturesJson = JsonSerializer.Serialize(request.Features ?? new()),
            DisplayOrder = request.DisplayOrder,
            IsFeatured = request.IsFeatured,
            IsActive = request.IsActive
        };

        _context.PortfolioProjects.Add(proj);
        await _context.SaveChangesAsync();
        return MapToPortfolioDto(proj);
    }

    public async Task<PortfolioProjectDto> UpdatePortfolioAsync(int id, UpdatePortfolioRequest request)
    {
        var proj = await _context.PortfolioProjects.FindAsync(id);
        if (proj == null)
            throw new NotFoundException($"Portfolio project #{id} not found.");

        proj.Title = request.Title.Trim();
        proj.Slug = string.IsNullOrWhiteSpace(request.Slug) ? GenerateSlug(request.Title) : request.Slug.Trim().ToLower();
        proj.ShortDescription = request.ShortDescription.Trim();
        proj.DetailedDescription = request.DetailedDescription.Trim();
        proj.Category = request.Category.Trim();
        proj.ThumbnailUrl = request.ThumbnailUrl;
        proj.LiveDemoUrl = request.LiveDemoUrl;
        proj.GitHubUrl = request.GitHubUrl;
        proj.TechnologiesJson = JsonSerializer.Serialize(request.Technologies ?? new());
        proj.FeaturesJson = JsonSerializer.Serialize(request.Features ?? new());
        proj.DisplayOrder = request.DisplayOrder;
        proj.IsFeatured = request.IsFeatured;
        proj.IsActive = request.IsActive;
        proj.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToPortfolioDto(proj);
    }

    public async Task<bool> DeletePortfolioAsync(int id)
    {
        var proj = await _context.PortfolioProjects.FindAsync(id);
        if (proj == null)
            throw new NotFoundException($"Portfolio project #{id} not found.");

        proj.IsDeleted = true;
        proj.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
    #endregion

    #region Blog
    public async Task<List<BlogPostDto>> GetBlogPostsAsync(string? categorySlug = null, int page = 1, int pageSize = 10)
    {
        var query = _context.BlogPosts
            .Include(p => p.BlogCategory)
            .Where(p => p.IsPublished)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(categorySlug))
            query = query.Where(p => p.BlogCategory != null && p.BlogCategory.Slug.ToLower() == categorySlug.ToLower());

        var posts = await query
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return posts.Select(MapToBlogPostDto).ToList();
    }

    public async Task<BlogPostDto> GetBlogPostBySlugAsync(string slug)
    {
        var post = await _context.BlogPosts
            .Include(p => p.BlogCategory)
            .FirstOrDefaultAsync(p => p.Slug.ToLower() == slug.ToLower());

        if (post == null)
            throw new NotFoundException($"Blog post '{slug}' not found.");

        post.ViewCount++;
        await _context.SaveChangesAsync();

        return MapToBlogPostDto(post);
    }

    public async Task<List<BlogCategoryDto>> GetBlogCategoriesAsync()
    {
        var cats = await _context.BlogCategories
            .Include(c => c.Posts)
            .AsNoTracking()
            .ToListAsync();

        return cats.Select(c => new BlogCategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            Description = c.Description,
            PostCount = c.Posts.Count(p => p.IsPublished && !p.IsDeleted)
        }).ToList();
    }

    public async Task<BlogPostDto> CreateBlogPostAsync(CreateBlogPostRequest request)
    {
        var post = new BlogPost
        {
            Title = request.Title.Trim(),
            Slug = string.IsNullOrWhiteSpace(request.Slug) ? GenerateSlug(request.Title) : request.Slug.Trim().ToLower(),
            Summary = request.Summary.Trim(),
            Content = request.Content.Trim(),
            BlogCategoryId = request.BlogCategoryId,
            ThumbnailUrl = request.ThumbnailUrl,
            AuthorName = string.IsNullOrWhiteSpace(request.AuthorName) ? "Technical Team" : request.AuthorName.Trim(),
            IsPublished = request.IsPublished,
            PublishedAt = request.IsPublished ? DateTime.UtcNow : null,
            TagsJson = JsonSerializer.Serialize(request.Tags ?? new())
        };

        _context.BlogPosts.Add(post);
        await _context.SaveChangesAsync();
        return MapToBlogPostDto(post);
    }

    public async Task<BlogPostDto> UpdateBlogPostAsync(int id, UpdateBlogPostRequest request)
    {
        var post = await _context.BlogPosts.FindAsync(id);
        if (post == null)
            throw new NotFoundException($"Blog post #{id} not found.");

        post.Title = request.Title.Trim();
        post.Slug = string.IsNullOrWhiteSpace(request.Slug) ? GenerateSlug(request.Title) : request.Slug.Trim().ToLower();
        post.Summary = request.Summary.Trim();
        post.Content = request.Content.Trim();
        post.BlogCategoryId = request.BlogCategoryId;
        post.ThumbnailUrl = request.ThumbnailUrl;
        post.AuthorName = request.AuthorName.Trim();
        if (!post.IsPublished && request.IsPublished && post.PublishedAt == null)
            post.PublishedAt = DateTime.UtcNow;
        post.IsPublished = request.IsPublished;
        post.TagsJson = JsonSerializer.Serialize(request.Tags ?? new());
        post.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToBlogPostDto(post);
    }

    public async Task<bool> DeleteBlogPostAsync(int id)
    {
        var post = await _context.BlogPosts.FindAsync(id);
        if (post == null)
            throw new NotFoundException($"Blog post #{id} not found.");

        post.IsDeleted = true;
        post.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
    #endregion

    #region Testimonials
    public async Task<List<TestimonialDto>> GetTestimonialsAsync(bool featuredOnly = false)
    {
        var query = _context.Testimonials.Where(t => t.IsActive).AsNoTracking();
        if (featuredOnly) query = query.Where(t => t.IsFeatured);

        var list = await query.OrderBy(t => t.DisplayOrder).ThenByDescending(t => t.CreatedAt).ToListAsync();
        return list.Select(t => new TestimonialDto
        {
            Id = t.Id,
            ClientName = t.ClientName,
            ClientRole = t.ClientRole,
            CompanyOrInstitution = t.CompanyOrInstitution,
            Feedback = t.Feedback,
            Rating = t.Rating,
            AvatarUrl = t.AvatarUrl,
            ProjectCategory = t.ProjectCategory,
            IsFeatured = t.IsFeatured,
            IsActive = t.IsActive,
            DisplayOrder = t.DisplayOrder,
            CreatedAt = t.CreatedAt
        }).ToList();
    }

    public async Task<TestimonialDto> CreateTestimonialAsync(CreateTestimonialRequest request)
    {
        var item = new Testimonial
        {
            ClientName = request.ClientName.Trim(),
            ClientRole = request.ClientRole.Trim(),
            CompanyOrInstitution = request.CompanyOrInstitution.Trim(),
            Feedback = request.Feedback.Trim(),
            Rating = Math.Clamp(request.Rating, 1, 5),
            AvatarUrl = request.AvatarUrl,
            ProjectCategory = request.ProjectCategory?.Trim(),
            IsFeatured = request.IsFeatured,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder
        };

        _context.Testimonials.Add(item);
        await _context.SaveChangesAsync();

        return new TestimonialDto
        {
            Id = item.Id,
            ClientName = item.ClientName,
            ClientRole = item.ClientRole,
            CompanyOrInstitution = item.CompanyOrInstitution,
            Feedback = item.Feedback,
            Rating = item.Rating,
            AvatarUrl = item.AvatarUrl,
            ProjectCategory = item.ProjectCategory,
            IsFeatured = item.IsFeatured,
            IsActive = item.IsActive,
            DisplayOrder = item.DisplayOrder,
            CreatedAt = item.CreatedAt
        };
    }
    #endregion

    #region Settings
    public async Task<Dictionary<string, string>> GetWebsiteSettingsAsync()
    {
        var settings = await _context.WebsiteSettings.AsNoTracking().ToListAsync();
        return settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);
    }

    public async Task<bool> UpdateSettingAsync(UpdateSettingRequest request)
    {
        await UpsertSingleSettingAsync(request.SettingKey, request.SettingValue);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateSettingsBatchAsync(Dictionary<string, string> settings)
    {
        foreach (var (key, value) in settings)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                await UpsertSingleSettingAsync(key, value ?? string.Empty);
            }
        }
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task UpsertSingleSettingAsync(string key, string value)
    {
        var setting = await _context.WebsiteSettings
            .FirstOrDefaultAsync(s => s.SettingKey.ToLower() == key.ToLower());

        if (setting == null)
        {
            _context.WebsiteSettings.Add(new WebsiteSetting
            {
                SettingKey = key,
                SettingValue = value,
                SettingGroup = key.StartsWith("Company:") ? "Contact" :
                               key.StartsWith("Finance:") ? "Finance" :
                               key.StartsWith("Quotation:") ? "Quotation" : "General"
            });
        }
        else
        {
            setting.SettingValue = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }
    }
    #endregion

    #region Helpers
    private static ServiceDto MapToServiceDto(ServiceItem s)
    {
        return new ServiceDto
        {
            Id = s.Id,
            ServiceCategoryId = s.ServiceCategoryId,
            ServiceCategoryName = s.ServiceCategory?.Name,
            Title = s.Title,
            Slug = s.Slug,
            ShortDescription = s.ShortDescription,
            FullDescription = s.FullDescription,
            Features = DeserializeList(s.FeaturesJson),
            Technologies = DeserializeList(s.TechnologiesJson),
            Benefits = DeserializeList(s.BenefitsJson),
            IconName = s.IconName,
            DisplayOrder = s.DisplayOrder,
            IsActive = s.IsActive,
            IsFeatured = s.IsFeatured
        };
    }

    private static PortfolioProjectDto MapToPortfolioDto(PortfolioProject p)
    {
        return new PortfolioProjectDto
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            ShortDescription = p.ShortDescription,
            DetailedDescription = p.DetailedDescription,
            Category = p.Category,
            ThumbnailUrl = p.ThumbnailUrl,
            LiveDemoUrl = p.LiveDemoUrl,
            GitHubUrl = p.GitHubUrl,
            Technologies = DeserializeList(p.TechnologiesJson),
            Features = DeserializeList(p.FeaturesJson),
            DisplayOrder = p.DisplayOrder,
            IsFeatured = p.IsFeatured,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        };
    }

    private static BlogPostDto MapToBlogPostDto(BlogPost p)
    {
        return new BlogPostDto
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            Summary = p.Summary,
            Content = p.Content,
            BlogCategoryId = p.BlogCategoryId,
            CategoryName = p.BlogCategory?.Name,
            ThumbnailUrl = p.ThumbnailUrl,
            AuthorName = p.AuthorName,
            IsPublished = p.IsPublished,
            PublishedAt = p.PublishedAt,
            ViewCount = p.ViewCount,
            Tags = DeserializeList(p.TagsJson),
            CreatedAt = p.CreatedAt
        };
    }

    private static List<string> DeserializeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private static string GenerateSlug(string text)
    {
        var clean = text.ToLowerInvariant().Trim();
        var chars = clean.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-').ToArray();
        var str = new string(chars).Replace(' ', '-');
        while (str.Contains("--")) str = str.Replace("--", "-");
        return str.Trim('-');
    }
    #endregion
}
