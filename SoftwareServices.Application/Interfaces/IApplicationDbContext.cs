using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SoftwareServices.Domain.Entities;

namespace SoftwareServices.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<AdminUser> AdminUsers { get; }
    DbSet<User> Users { get; }
    DbSet<Customer> Customers { get; }
    DbSet<ServiceCategory> ServiceCategories { get; }
    DbSet<ServiceItem> Services { get; }
    DbSet<Enquiry> Enquiries { get; }
    DbSet<EnquiryAttachment> EnquiryAttachments { get; }
    DbSet<EnquiryNote> EnquiryNotes { get; }
    DbSet<Quotation> Quotations { get; }
    DbSet<QuotationItem> QuotationItems { get; }
    DbSet<QuotationStatusHistory> QuotationStatusHistories { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectMilestone> ProjectMilestones { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PortfolioProject> PortfolioProjects { get; }
    DbSet<BlogCategory> BlogCategories { get; }
    DbSet<BlogPost> BlogPosts { get; }
    DbSet<Testimonial> Testimonials { get; }
    DbSet<WebsiteSetting> WebsiteSettings { get; }
    DbSet<AppConfiguration> AppConfigurations { get; }
    DbSet<EmailOutbox> EmailOutbox { get; }
    DbSet<EmailLog> EmailLogs { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
