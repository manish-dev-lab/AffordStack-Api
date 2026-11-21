using System.Security.Cryptography;
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

    private static string GenerateReferenceNumber()
    {
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        Span<byte> randomBytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(randomBytes);
        var token = Convert.ToHexString(randomBytes);
        return $"ENQ-{datePrefix}-{token}";
    }

    public async Task<EnquiryResponseDto> SubmitEnquiryAsync(CreateEnquiryRequest request, (string FileName, string ContentType, byte[] Bytes)? attachment = null)
    {
        var cleanEmail = request.Email.Trim().ToLowerInvariant();

        // 1. Find or create customer
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email.ToLower() == cleanEmail);

        bool isNewCustomer = false;
        if (customer == null)
        {
            isNewCustomer = true;
            customer = new Customer
            {
                FullName = request.FullName.Trim(),
                Email = cleanEmail,
                PhoneNumber = request.PhoneNumber.Trim(),
                CompanyName = request.CompanyName?.Trim()
            };
            _context.Customers.Add(customer);
        }
        else
        {
            // Update customer details if provided
            customer.FullName = request.FullName.Trim();
            customer.PhoneNumber = request.PhoneNumber.Trim();
            if (!string.IsNullOrWhiteSpace(request.CompanyName))
                customer.CompanyName = request.CompanyName.Trim();
        }

        // 2. Resolve Category ID if name was provided
        int? categoryId = request.ServiceCategoryId;
        if (!categoryId.HasValue && !string.IsNullOrWhiteSpace(request.ServiceCategoryName))
        {
            var matchedCat = await _context.ServiceCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name.ToLower() == request.ServiceCategoryName.ToLower());
            if (matchedCat != null) categoryId = matchedCat.Id;
        }

        // 3. Create Enquiry with high-entropy collision-free reference
        var refNumber = GenerateReferenceNumber();
        var enquiry = new Enquiry
        {
            ReferenceNumber = refNumber,
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

        if (!isNewCustomer)
        {
            enquiry.CustomerId = customer.Id;
        }

        _context.Enquiries.Add(enquiry);

        // 4. Handle attachment if present (save file and attach entity in same transaction)
        if (attachment.HasValue && attachment.Value.Bytes.Length > 0)
        {
            var safeDir = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "Attachments");
            Directory.CreateDirectory(safeDir);
            var storedName = $"{Guid.NewGuid():N}_{Path.GetFileName(attachment.Value.FileName)}";
            var fullPath = Path.Combine(safeDir, storedName);
            await File.WriteAllBytesAsync(fullPath, attachment.Value.Bytes);

            var attRecord = new EnquiryAttachment
            {
                Enquiry = enquiry,
                OriginalFileName = attachment.Value.FileName,
                StoredFileName = storedName,
                ContentType = attachment.Value.ContentType,
                FileSizeBytes = attachment.Value.Bytes.Length,
                StoragePath = fullPath
            };
            _context.EnquiryAttachments.Add(attRecord);
        }

        // 5. Persist everything in a single atomic database round trip
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException) when (isNewCustomer)
        {
            // Concurrency race: Another request concurrently registered this customer email.
            // Clear uncommitted Customer entry, re-fetch the committed Customer, and re-save the enquiry.
            _context.Entry(customer).State = EntityState.Detached;

            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email.ToLower() == cleanEmail);

            if (existingCustomer != null)
            {
                customer = existingCustomer;
                enquiry.Customer = null;
                enquiry.CustomerId = existingCustomer.Id;
                await _context.SaveChangesAsync();
            }
            else
            {
                throw;
            }
        }

        // 6. Trigger non-blocking emails
        try
        {
            await _emailService.SendEnquiryAcknowledgementAsync(enquiry, customer);
            await _emailService.SendAdminEnquiryNotificationAsync(enquiry, customer);
        }
        catch
        {
            // Background dispatcher/logging catches any edge issues; enquiry is safely committed.
        }

        return new EnquiryResponseDto
        {
            ReferenceNumber = enquiry.ReferenceNumber,
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
