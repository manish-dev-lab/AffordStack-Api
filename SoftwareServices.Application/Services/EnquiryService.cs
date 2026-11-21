using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.Common.Exceptions;
using SoftwareServices.Application.DTOs.Enquiry;
using SoftwareServices.Application.DTOs.Quotation;
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

        // Link registered user if account exists for this email
        var registeredUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == cleanEmail);
        if (registeredUser != null)
        {
            enquiry.UserId = registeredUser.Id;
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

        // Synchronize linked projects when admin updates enquiry status
        if (request.Status == EnquiryStatus.Closed)
        {
            var linkedProjects = await _context.Projects
                .Where(p => _context.Quotations.Any(q => q.EnquiryId == enquiry.Id && q.Id == p.QuotationId))
                .ToListAsync();
            foreach (var p in linkedProjects)
            {
                p.Status = ProjectStatus.Completed;
                p.ProgressPercentage = 100;
                p.ActualCompletionDate ??= DateTime.UtcNow;
                p.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (request.Status == EnquiryStatus.Converted)
        {
            var linkedProjects = await _context.Projects
                .Where(p => _context.Quotations.Any(q => q.EnquiryId == enquiry.Id && q.Id == p.QuotationId))
                .ToListAsync();
            foreach (var p in linkedProjects)
            {
                if (p.Status == ProjectStatus.NotStarted)
                {
                    p.Status = ProjectStatus.InProgress;
                    p.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            int? validAdminId = null;
            if (adminUserId.HasValue && adminUserId.Value > 0)
            {
                var adminExists = await _context.AdminUsers.AnyAsync(a => a.Id == adminUserId.Value);
                if (adminExists) validAdminId = adminUserId.Value;
            }

            _context.EnquiryNotes.Add(new EnquiryNote
            {
                EnquiryId = enquiry.Id,
                AdminUserId = validAdminId,
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

    public async Task<EnquiryTrackingDto> TrackEnquiryAsync(TrackEnquiryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReferenceNumber) || string.IsNullOrWhiteSpace(request.Email))
            throw new BadRequestException("Please provide both your Reference Number and Email address.");

        var cleanRef = request.ReferenceNumber.Trim().ToUpperInvariant();
        var cleanEmail = request.Email.Trim().ToLowerInvariant();

        var enquiry = await _context.Enquiries
            .Include(e => e.Customer)
            .Include(e => e.ServiceCategory)
            .Include(e => e.Quotations)
                .ThenInclude(q => q.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.ReferenceNumber.ToUpper() == cleanRef &&
                e.Customer != null && e.Customer.Email.ToLower() == cleanEmail);

        if (enquiry == null)
            throw new NotFoundException($"No enquiry was found matching Reference '{cleanRef}' and Email '{cleanEmail}'. Please verify your credentials.");

        // Determine step index and status descriptions
        var (display, desc, step) = enquiry.Status switch
        {
            EnquiryStatus.New => ("Submitted", "Your enquiry has been successfully logged into our engineering queue.", 1),
            EnquiryStatus.Reviewed => ("Under Technical Review", "Our architects are evaluating your project requirements and estimating deliverables.", 2),
            EnquiryStatus.Quoted => ("Quotation Ready", "Your official quotation proposal is ready. You can review pricing, milestones, deliverables, and choose to accept or decline below.", 3),
            EnquiryStatus.FollowUp => ("In Follow-Up / Discussion", "We are discussing project specifics and clarifications with you.", 2),
            EnquiryStatus.Converted => ("Project Provisioned", "Quotation accepted! Your project repository, milestones, and development sprint have been initiated.", 4),
            EnquiryStatus.Closed => ("Completed / Closed", "This enquiry has completed its lifecycle.", 5),
            EnquiryStatus.Spam => ("Archived", "This submission has been archived.", 1),
            _ => ("Processing", "Your request is currently being handled.", 1)
        };

        // Find active quotation (latest non-draft quotation, or latest if draft)
        PublicQuotationDto? activeQuoteDto = null;
        var activeQuote = enquiry.Quotations
            .Where(q => !q.IsDeleted && q.Status != QuotationStatus.Draft)
            .OrderByDescending(q => q.CreatedAt)
            .FirstOrDefault()
            ?? enquiry.Quotations
                .Where(q => !q.IsDeleted)
                .OrderByDescending(q => q.CreatedAt)
                .FirstOrDefault();

        if (activeQuote != null)
        {
            if (activeQuote.Status == QuotationStatus.Accepted)
            {
                step = 4;
                display = "Quotation Accepted & Project Provisioned";
                desc = "You accepted this quotation proposal! The engineering team is actively developing the project deliverables.";
            }
            else if (activeQuote.Status == QuotationStatus.Rejected)
            {
                display = "Quotation Declined";
                desc = "You declined this quotation proposal. You may reach out if you would like revised terms or scope.";
            }
            else if (step < 3)
            {
                step = 3;
                display = "Quotation Ready";
                desc = "Your official quotation proposal is ready for your review and approval!";
            }

            activeQuoteDto = new PublicQuotationDto
            {
                QuotationNumber = activeQuote.QuotationNumber,
                SecureToken = activeQuote.SecureToken,
                CustomerName = enquiry.Customer?.FullName ?? "Valued Client",
                CompanyName = enquiry.Customer?.CompanyName,
                ServiceDescription = activeQuote.ServiceDescription,
                Deliverables = activeQuote.Deliverables,
                DevelopmentCost = activeQuote.DevelopmentCost,
                AdditionalCost = activeQuote.AdditionalCost,
                TaxRate = activeQuote.TaxRate,
                TaxAmount = activeQuote.TaxAmount,
                TotalAmount = activeQuote.TotalAmount,
                AdvancePaymentAmount = activeQuote.AdvancePaymentAmount,
                RemainingPaymentAmount = activeQuote.RemainingPaymentAmount,
                ExpectedDeliveryDate = activeQuote.ExpectedDeliveryDate,
                ValidUntil = activeQuote.ValidUntil,
                TermsAndConditions = activeQuote.TermsAndConditions,
                Status = activeQuote.Status,
                RespondedAt = activeQuote.RespondedAt,
                ResponseRemarks = activeQuote.ResponseRemarks,
                Items = activeQuote.Items.Select(i => new QuotationItemDto
                {
                    Id = i.Id,
                    Title = i.Title,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            };
        }

        Project? project = null;
        if (activeQuote != null)
        {
            project = await _context.Projects
                .Include(p => p.Milestones)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.QuotationId == activeQuote.Id);
        }

        if (project != null)
        {
            if (project.Status == ProjectStatus.Completed || enquiry.Status == EnquiryStatus.Closed)
            {
                step = 5;
                display = "Delivered (Handed Over)";
                desc = "Codebase, demo & documentation have been handed over successfully!";
            }
            else if (project.Status == ProjectStatus.InProgress)
            {
                step = 4;
                display = "Project Active & In Progress";
                desc = "Your project is actively being developed according to milestones.";
            }
            else if (project.Status == ProjectStatus.OnHold)
            {
                step = 4;
                display = "Project On Hold";
                desc = "Project development is temporarily paused.";
            }
            else if (project.Status == ProjectStatus.Cancelled)
            {
                step = 4;
                display = "Project Cancelled";
                desc = "Project development has been cancelled.";
            }
        }
        else if (enquiry.Status == EnquiryStatus.Closed)
        {
            step = 5;
            display = "Delivered (Handed Over)";
            desc = "Codebase, demo & documentation have been handed over successfully!";
        }

        return new EnquiryTrackingDto
        {
            Id = enquiry.Id,
            ReferenceNumber = enquiry.ReferenceNumber,
            CustomerName = enquiry.Customer?.FullName ?? "",
            CustomerEmail = enquiry.Customer?.Email ?? "",
            CustomerPhone = enquiry.Customer?.PhoneNumber ?? "",
            CompanyName = enquiry.Customer?.CompanyName,
            ProjectTitle = enquiry.ProjectTitle,
            ProjectDescription = enquiry.ProjectDescription,
            ServiceCategoryName = enquiry.ServiceCategory?.Name ?? "Custom Engineering",
            RequiredTechnology = enquiry.RequiredTechnology,
            EstimatedBudget = enquiry.EstimatedBudget,
            Status = enquiry.Status,
            StatusDisplay = display,
            StatusDescription = desc,
            StepIndex = step,
            CreatedAt = enquiry.CreatedAt,
            UpdatedAt = enquiry.UpdatedAt,
            ActiveQuotation = activeQuoteDto,
            ProjectId = project?.Id,
            ProjectNumber = project?.ProjectNumber,
            ProjectStatus = project?.Status,
            ProgressPercentage = project?.ProgressPercentage,
            ExpectedCompletionDate = project?.ExpectedCompletionDate ?? activeQuote?.ExpectedDeliveryDate,
            Milestones = project?.Milestones.OrderBy(m => m.DueDate).Select(m => new SoftwareServices.Application.DTOs.Project.ProjectMilestoneDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                DueDate = m.DueDate,
                IsCompleted = m.IsCompleted,
                CompletedAt = m.CompletedAt,
                Amount = m.Amount
            }).ToList() ?? new()
        };
    }
}
