using Microsoft.EntityFrameworkCore;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.Common.Exceptions;
using SoftwareServices.Application.DTOs.Enquiry;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Domain.Entities;
using SoftwareServices.Domain.Enums;

namespace SoftwareServices.Application.Services;

public class EnquiryService : IEnquiryService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public EnquiryService(IApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<EnquiryResponseDto> SubmitEnquiryAsync(CreateEnquiryRequest request, (string FileName, string ContentType, byte[] Bytes)? attachment = null)
    {
        var cleanEmail = request.Email.Trim().ToLower();

        // Find or create customer
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email.ToLower() == cleanEmail);

        if (customer == null)
        {
            customer = new Customer
            {
                FullName = request.FullName.Trim(),
                Email = cleanEmail,
                PhoneNumber = request.PhoneNumber.Trim(),
                CompanyName = request.CompanyName?.Trim()
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Update customer details if provided
            customer.FullName = request.FullName.Trim();
            customer.PhoneNumber = request.PhoneNumber.Trim();
            if (!string.IsNullOrWhiteSpace(request.CompanyName))
                customer.CompanyName = request.CompanyName.Trim();
        }

        // Generate Unique Reference Number: ENQ-yyyyMMdd-XXXX
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var countToday = await _context.Enquiries
            .CountAsync(e => e.ReferenceNumber.StartsWith($"ENQ-{datePrefix}"));
        var refNumber = $"ENQ-{datePrefix}-{(countToday + 1):D4}";

        // Find category if ID is missing but name is provided
        int? categoryId = request.ServiceCategoryId;
        if (!categoryId.HasValue && !string.IsNullOrWhiteSpace(request.ServiceCategoryName))
        {
            var matchedCat = await _context.ServiceCategories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == request.ServiceCategoryName.ToLower());
            if (matchedCat != null) categoryId = matchedCat.Id;
        }

        var enquiry = new Enquiry
        {
            ReferenceNumber = refNumber,
            CustomerId = customer.Id,
            Customer = customer,
            ServiceCategoryId = categoryId,
            RequiredTechnology = request.RequiredTechnology.Trim(),
            ProjectTitle = request.ProjectTitle.Trim(),
            ProjectDescription = request.ProjectDescription.Trim(),
            EstimatedBudget = request.EstimatedBudget.Trim(),
            ExpectedCompletionDate = request.ExpectedCompletionDate,
            AdditionalRequirements = request.AdditionalRequirements?.Trim(),
            Status = EnquiryStatus.New
        };

        _context.Enquiries.Add(enquiry);
        await _context.SaveChangesAsync();

        // Handle attachment if any
        if (attachment.HasValue && attachment.Value.Bytes.Length > 0)
        {
            var safeDir = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "Attachments");
            Directory.CreateDirectory(safeDir);
            var storedName = $"{Guid.NewGuid():N}_{Path.GetFileName(attachment.Value.FileName)}";
            var fullPath = Path.Combine(safeDir, storedName);
            await File.WriteAllBytesAsync(fullPath, attachment.Value.Bytes);

            var attRecord = new EnquiryAttachment
            {
                EnquiryId = enquiry.Id,
                OriginalFileName = attachment.Value.FileName,
                StoredFileName = storedName,
                ContentType = attachment.Value.ContentType,
                FileSizeBytes = attachment.Value.Bytes.Length,
                StoragePath = fullPath
            };
            _context.EnquiryAttachments.Add(attRecord);
            await _context.SaveChangesAsync();
        }

        // Trigger emails safely (background/outbox handles resilience)
        try
        {
            await _emailService.SendEnquiryAcknowledgementAsync(enquiry, customer);
            await _emailService.SendAdminEnquiryNotificationAsync(enquiry, customer);
        }
        catch
        {
            // Logging occurs inside email service, enquiry is safely committed
        }

        return new EnquiryResponseDto
        {
            ReferenceNumber = refNumber,
            Message = "Your quotation enquiry has been submitted successfully! We have sent a confirmation email to your inbox.",
            CreatedAt = enquiry.CreatedAt
        };
    }

    public async Task<PagedResult<EnquiryListDto>> GetEnquiriesAsync(int page = 1, int pageSize = 10, EnquiryStatus? status = null, int? serviceCategoryId = null, string? search = null)
    {
        var query = _context.Enquiries
            .Include(e => e.Customer)
            .Include(e => e.ServiceCategory)
            .Include(e => e.Notes)
            .Include(e => e.Quotations)
            .AsNoTracking();

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        if (serviceCategoryId.HasValue)
            query = query.Where(e => e.ServiceCategoryId == serviceCategoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(e => e.ReferenceNumber.ToLower().Contains(s)
                || e.ProjectTitle.ToLower().Contains(s)
                || e.Customer!.FullName.ToLower().Contains(s)
                || e.Customer.Email.ToLower().Contains(s));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EnquiryListDto
            {
                Id = e.Id,
                ReferenceNumber = e.ReferenceNumber,
                CustomerName = e.Customer!.FullName,
                CustomerEmail = e.Customer.Email,
                CustomerPhone = e.Customer.PhoneNumber,
                CompanyName = e.Customer.CompanyName,
                ServiceCategoryName = e.ServiceCategory != null ? e.ServiceCategory.Name : "General",
                RequiredTechnology = e.RequiredTechnology,
                ProjectTitle = e.ProjectTitle,
                EstimatedBudget = e.EstimatedBudget,
                Status = e.Status,
                CreatedAt = e.CreatedAt,
                NotesCount = e.Notes.Count,
                QuotationsCount = e.Quotations.Count
            })
            .ToListAsync();

        return new PagedResult<EnquiryListDto>(items, total, page, pageSize);
    }

    public async Task<EnquiryDetailDto> GetEnquiryByIdAsync(int id)
    {
        var enquiry = await _context.Enquiries
            .Include(e => e.Customer)
            .Include(e => e.ServiceCategory)
            .Include(e => e.Attachments)
            .Include(e => e.Notes)
                .ThenInclude(n => n.AdminUser)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enquiry == null)
            throw new NotFoundException($"Enquiry #{id} not found.");

        return new EnquiryDetailDto
        {
            Id = enquiry.Id,
            ReferenceNumber = enquiry.ReferenceNumber,
            CustomerId = enquiry.CustomerId,
            CustomerName = enquiry.Customer!.FullName,
            CustomerEmail = enquiry.Customer.Email,
            CustomerPhone = enquiry.Customer.PhoneNumber,
            CompanyName = enquiry.Customer.CompanyName,
            ServiceCategoryId = enquiry.ServiceCategoryId,
            ServiceCategoryName = enquiry.ServiceCategory?.Name,
            RequiredTechnology = enquiry.RequiredTechnology,
            ProjectTitle = enquiry.ProjectTitle,
            ProjectDescription = enquiry.ProjectDescription,
            EstimatedBudget = enquiry.EstimatedBudget,
            ExpectedCompletionDate = enquiry.ExpectedCompletionDate,
            AdditionalRequirements = enquiry.AdditionalRequirements,
            Status = enquiry.Status,
            CreatedAt = enquiry.CreatedAt,
            Attachments = enquiry.Attachments.Select(a => new EnquiryAttachmentDto
            {
                Id = a.Id,
                OriginalFileName = a.OriginalFileName,
                ContentType = a.ContentType,
                FileSizeBytes = a.FileSizeBytes,
                CreatedAt = a.CreatedAt
            }).ToList(),
            Notes = enquiry.Notes.OrderByDescending(n => n.CreatedAt).Select(n => new EnquiryNoteDto
            {
                Id = n.Id,
                AuthorName = n.AdminUser?.FullName ?? "Admin",
                NoteContent = n.NoteContent,
                CreatedAt = n.CreatedAt
            }).ToList()
        };
    }

    public async Task<bool> UpdateStatusAsync(int id, UpdateEnquiryStatusRequest request, int? adminUserId = null)
    {
        var enquiry = await _context.Enquiries.FindAsync(id);
        if (enquiry == null)
            throw new NotFoundException($"Enquiry #{id} not found.");

        enquiry.Status = request.Status;
        enquiry.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            _context.EnquiryNotes.Add(new EnquiryNote
            {
                EnquiryId = enquiry.Id,
                AdminUserId = adminUserId,
                NoteContent = $"Status changed to {request.Status}: {request.Note}"
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<EnquiryNoteDto> AddNoteAsync(int id, AddEnquiryNoteRequest request, int? adminUserId = null)
    {
        var enquiry = await _context.Enquiries.FindAsync(id);
        if (enquiry == null)
            throw new NotFoundException($"Enquiry #{id} not found.");

        var note = new EnquiryNote
        {
            EnquiryId = enquiry.Id,
            AdminUserId = adminUserId,
            NoteContent = request.NoteContent.Trim()
        };

        _context.EnquiryNotes.Add(note);
        await _context.SaveChangesAsync();

        string authorName = "Admin";
        if (adminUserId.HasValue)
        {
            var user = await _context.AdminUsers.FindAsync(adminUserId.Value);
            if (user != null) authorName = user.FullName;
        }

        return new EnquiryNoteDto
        {
            Id = note.Id,
            AuthorName = authorName,
            NoteContent = note.NoteContent,
            CreatedAt = note.CreatedAt
        };
    }

    public async Task<bool> DeleteEnquiryAsync(int id)
    {
        var enquiry = await _context.Enquiries.FindAsync(id);
        if (enquiry == null)
            throw new NotFoundException($"Enquiry #{id} not found.");

        enquiry.IsDeleted = true;
        enquiry.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
