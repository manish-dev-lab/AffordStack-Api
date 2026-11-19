using Microsoft.EntityFrameworkCore;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Domain.Entities;

namespace SoftwareServices.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<ServiceItem> Services => Set<ServiceItem>();
    public DbSet<Enquiry> Enquiries => Set<Enquiry>();
    public DbSet<EnquiryAttachment> EnquiryAttachments => Set<EnquiryAttachment>();
    public DbSet<EnquiryNote> EnquiryNotes => Set<EnquiryNote>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();
    public DbSet<QuotationStatusHistory> QuotationStatusHistories => Set<QuotationStatusHistory>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMilestone> ProjectMilestones => Set<ProjectMilestone>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PortfolioProject> PortfolioProjects => Set<PortfolioProject>();
    public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<WebsiteSetting> WebsiteSettings => Set<WebsiteSetting>();
    public DbSet<EmailOutbox> EmailOutbox => Set<EmailOutbox>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global query filter for soft delete
        modelBuilder.Entity<Customer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Enquiry>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Quotation>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Project>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ServiceCategory>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ServiceItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PortfolioProject>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<BlogPost>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Testimonial>().HasQueryFilter(e => !e.IsDeleted);

        // AdminUser
        modelBuilder.Entity<AdminUser>(b =>
        {
            b.ToTable("admin_users");
            b.HasIndex(u => u.Username).IsUnique();
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.Username).HasMaxLength(100).IsRequired();
            b.Property(u => u.Email).HasMaxLength(150).IsRequired();
            b.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            b.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
        });

        // Customer
        modelBuilder.Entity<Customer>(b =>
        {
            b.ToTable("customers");
            b.HasIndex(c => c.Email).IsUnique();
            b.Property(c => c.FullName).HasMaxLength(150).IsRequired();
            b.Property(c => c.Email).HasMaxLength(150).IsRequired();
            b.Property(c => c.PhoneNumber).HasMaxLength(50).IsRequired();
            b.Property(c => c.CompanyName).HasMaxLength(150);
        });

        // ServiceCategory
        modelBuilder.Entity<ServiceCategory>(b =>
        {
            b.ToTable("service_categories");
            b.HasIndex(c => c.Slug).IsUnique();
            b.Property(c => c.Name).HasMaxLength(150).IsRequired();
            b.Property(c => c.Slug).HasMaxLength(150).IsRequired();
            b.Property(c => c.Description).HasMaxLength(1000);
            b.Property(c => c.IconName).HasMaxLength(50);
        });

        // ServiceItem
        modelBuilder.Entity<ServiceItem>(b =>
        {
            b.ToTable("services");
            b.HasIndex(s => s.Slug).IsUnique();
            b.Property(s => s.Title).HasMaxLength(200).IsRequired();
            b.Property(s => s.Slug).HasMaxLength(200).IsRequired();
            b.Property(s => s.ShortDescription).HasMaxLength(500).IsRequired();
            b.Property(s => s.IconName).HasMaxLength(50);
            b.HasOne(s => s.ServiceCategory)
             .WithMany(c => c.Services)
             .HasForeignKey(s => s.ServiceCategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Enquiry
        modelBuilder.Entity<Enquiry>(b =>
        {
            b.ToTable("enquiries");
            b.HasIndex(e => e.ReferenceNumber).IsUnique();
            b.HasIndex(e => e.Status);
            b.HasIndex(e => e.CreatedAt);
            b.Property(e => e.ReferenceNumber).HasMaxLength(50).IsRequired();
            b.Property(e => e.ProjectTitle).HasMaxLength(250).IsRequired();
            b.Property(e => e.RequiredTechnology).HasMaxLength(150).IsRequired();
            b.Property(e => e.EstimatedBudget).HasMaxLength(100).IsRequired();

            b.HasOne(e => e.Customer)
             .WithMany(c => c.Enquiries)
             .HasForeignKey(e => e.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.ServiceCategory)
             .WithMany()
             .HasForeignKey(e => e.ServiceCategoryId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // EnquiryAttachment
        modelBuilder.Entity<EnquiryAttachment>(b =>
        {
            b.ToTable("enquiry_attachments");
            b.Property(a => a.OriginalFileName).HasMaxLength(255).IsRequired();
            b.Property(a => a.StoredFileName).HasMaxLength(255).IsRequired();
            b.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
            b.Property(a => a.StoragePath).HasMaxLength(500).IsRequired();

            b.HasOne(a => a.Enquiry)
             .WithMany(e => e.Attachments)
             .HasForeignKey(a => a.EnquiryId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // EnquiryNote
        modelBuilder.Entity<EnquiryNote>(b =>
        {
            b.ToTable("enquiry_notes");
            b.HasOne(n => n.Enquiry)
             .WithMany(e => e.Notes)
             .HasForeignKey(n => n.EnquiryId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(n => n.AdminUser)
             .WithMany()
             .HasForeignKey(n => n.AdminUserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // Quotation
        modelBuilder.Entity<Quotation>(b =>
        {
            b.ToTable("quotations");
            b.HasIndex(q => q.QuotationNumber).IsUnique();
            b.HasIndex(q => q.SecureToken).IsUnique();
            b.HasIndex(q => q.Status);
            b.Property(q => q.QuotationNumber).HasMaxLength(50).IsRequired();
            b.Property(q => q.SecureToken).HasMaxLength(100).IsRequired();
            
            b.Property(q => q.DevelopmentCost).HasPrecision(18, 2);
            b.Property(q => q.AdditionalCost).HasPrecision(18, 2);
            b.Property(q => q.TaxRate).HasPrecision(5, 2);
            b.Property(q => q.TaxAmount).HasPrecision(18, 2);
            b.Property(q => q.TotalAmount).HasPrecision(18, 2);
            b.Property(q => q.AdvancePaymentAmount).HasPrecision(18, 2);
            b.Property(q => q.RemainingPaymentAmount).HasPrecision(18, 2);

            b.HasOne(q => q.Enquiry)
             .WithMany(e => e.Quotations)
             .HasForeignKey(q => q.EnquiryId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(q => q.Customer)
             .WithMany(c => c.Quotations)
             .HasForeignKey(q => q.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // QuotationItem
        modelBuilder.Entity<QuotationItem>(b =>
        {
            b.ToTable("quotation_items");
            b.Property(i => i.Title).HasMaxLength(200).IsRequired();
            b.Property(i => i.UnitPrice).HasPrecision(18, 2);
            b.Property(i => i.TotalPrice).HasPrecision(18, 2);

            b.HasOne(i => i.Quotation)
             .WithMany(q => q.Items)
             .HasForeignKey(i => i.QuotationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // QuotationStatusHistory
        modelBuilder.Entity<QuotationStatusHistory>(b =>
        {
            b.ToTable("quotation_status_histories");
            b.Property(h => h.ChangedBy).HasMaxLength(100);

            b.HasOne(h => h.Quotation)
             .WithMany(q => q.StatusHistory)
             .HasForeignKey(h => h.QuotationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Project
        modelBuilder.Entity<Project>(b =>
        {
            b.ToTable("projects");
            b.HasIndex(p => p.ProjectNumber).IsUnique();
            b.HasIndex(p => p.Status);
            b.Property(p => p.ProjectNumber).HasMaxLength(50).IsRequired();
            b.Property(p => p.ProjectTitle).HasMaxLength(250).IsRequired();
            b.Property(p => p.AgreedAmount).HasPrecision(18, 2);

            b.HasOne(p => p.Customer)
             .WithMany(c => c.Projects)
             .HasForeignKey(p => p.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.Quotation)
             .WithOne(q => q.Project)
             .HasForeignKey<Project>(p => p.QuotationId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ProjectMilestone
        modelBuilder.Entity<ProjectMilestone>(b =>
        {
            b.ToTable("project_milestones");
            b.Property(m => m.Title).HasMaxLength(200).IsRequired();
            b.Property(m => m.Amount).HasPrecision(18, 2);

            b.HasOne(m => m.Project)
             .WithMany(p => p.Milestones)
             .HasForeignKey(m => m.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Payment
        modelBuilder.Entity<Payment>(b =>
        {
            b.ToTable("payments");
            b.Property(p => p.Amount).HasPrecision(18, 2);
            b.Property(p => p.PaymentMethod).HasMaxLength(100).IsRequired();
            b.Property(p => p.TransactionReference).HasMaxLength(150);

            b.HasOne(p => p.Project)
             .WithMany(pr => pr.Payments)
             .HasForeignKey(p => p.ProjectId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.Customer)
             .WithMany()
             .HasForeignKey(p => p.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // PortfolioProject
        modelBuilder.Entity<PortfolioProject>(b =>
        {
            b.ToTable("portfolio_projects");
            b.HasIndex(p => p.Slug).IsUnique();
            b.Property(p => p.Title).HasMaxLength(200).IsRequired();
            b.Property(p => p.Slug).HasMaxLength(200).IsRequired();
            b.Property(p => p.Category).HasMaxLength(100).IsRequired();
            b.Property(p => p.ThumbnailUrl).HasMaxLength(500);
            b.Property(p => p.LiveDemoUrl).HasMaxLength(500);
            b.Property(p => p.GitHubUrl).HasMaxLength(500);
        });

        // BlogCategory
        modelBuilder.Entity<BlogCategory>(b =>
        {
            b.ToTable("blog_categories");
            b.HasIndex(c => c.Slug).IsUnique();
            b.Property(c => c.Name).HasMaxLength(100).IsRequired();
            b.Property(c => c.Slug).HasMaxLength(100).IsRequired();
        });

        // BlogPost
        modelBuilder.Entity<BlogPost>(b =>
        {
            b.ToTable("blog_posts");
            b.HasIndex(p => p.Slug).IsUnique();
            b.HasIndex(p => p.IsPublished);
            b.Property(p => p.Title).HasMaxLength(250).IsRequired();
            b.Property(p => p.Slug).HasMaxLength(250).IsRequired();
            b.Property(p => p.AuthorName).HasMaxLength(100);
            b.Property(p => p.ThumbnailUrl).HasMaxLength(500);

            b.HasOne(p => p.BlogCategory)
             .WithMany(c => c.Posts)
             .HasForeignKey(p => p.BlogCategoryId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // Testimonial
        modelBuilder.Entity<Testimonial>(b =>
        {
            b.ToTable("testimonials");
            b.Property(t => t.ClientName).HasMaxLength(150).IsRequired();
            b.Property(t => t.ClientRole).HasMaxLength(150);
            b.Property(t => t.CompanyOrInstitution).HasMaxLength(150);
            b.Property(t => t.AvatarUrl).HasMaxLength(500);
            b.Property(t => t.ProjectCategory).HasMaxLength(100);
        });

        // WebsiteSetting
        modelBuilder.Entity<WebsiteSetting>(b =>
        {
            b.ToTable("website_settings");
            b.HasIndex(s => s.SettingKey).IsUnique();
            b.Property(s => s.SettingKey).HasMaxLength(100).IsRequired();
            b.Property(s => s.SettingGroup).HasMaxLength(50).IsRequired();
            b.Property(s => s.Description).HasMaxLength(250);
        });

        // EmailOutbox
        modelBuilder.Entity<EmailOutbox>(b =>
        {
            b.ToTable("email_outbox");
            b.HasIndex(e => e.Status);
            b.Property(e => e.RecipientEmail).HasMaxLength(150).IsRequired();
            b.Property(e => e.RecipientName).HasMaxLength(150);
            b.Property(e => e.Subject).HasMaxLength(250).IsRequired();
        });

        // EmailLog
        modelBuilder.Entity<EmailLog>(b =>
        {
            b.ToTable("email_logs");
            b.Property(e => e.RecipientEmail).HasMaxLength(150).IsRequired();
            b.Property(e => e.Subject).HasMaxLength(250).IsRequired();
            b.Property(e => e.EventType).HasMaxLength(100).IsRequired();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Domain.Common.BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (Domain.Common.BaseEntity)entry.Entity;
            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
