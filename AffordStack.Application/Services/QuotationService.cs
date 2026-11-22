using Microsoft.EntityFrameworkCore;
using AffordStack.Application.Common;
using AffordStack.Application.Common.Exceptions;
using AffordStack.Application.DTOs.Quotation;
using AffordStack.Application.Interfaces;
using AffordStack.Domain.Entities;
using AffordStack.Domain.Enums;

namespace AffordStack.Application.Services;

public class QuotationService : IQuotationService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IPdfQuotationGenerator _pdfGenerator;

    public QuotationService(
        IApplicationDbContext context,
        IEmailService emailService,
        IPdfQuotationGenerator pdfGenerator)
    {
        _context = context;
        _emailService = emailService;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<QuotationDto> CreateQuotationAsync(CreateQuotationRequest request, string authorName = "Admin")
    {
        var enquiry = await _context.Enquiries
            .Include(e => e.Customer)
            .FirstOrDefaultAsync(e => e.Id == request.EnquiryId);

        if (enquiry == null)
            throw new NotFoundException($"Enquiry #{request.EnquiryId} not found.");

        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var countToday = await _context.Quotations
            .CountAsync(q => q.QuotationNumber.StartsWith($"QT-{datePrefix}"));
        var quoteNumber = $"QT-{datePrefix}-{(countToday + 1):D4}";

        var subtotal = request.DevelopmentCost + request.AdditionalCost;
        var taxAmount = Math.Round(subtotal * (request.TaxRate / 100m), 2);
        var totalAmount = subtotal + taxAmount;
        var advance = request.AdvancePaymentAmount > 0 ? request.AdvancePaymentAmount : Math.Round(totalAmount * 0.40m, 2);
        var remaining = totalAmount - advance;

        var quotation = new Quotation
        {
            QuotationNumber = quoteNumber,
            EnquiryId = enquiry.Id,
            CustomerId = enquiry.CustomerId,
            Customer = enquiry.Customer,
            ServiceDescription = request.ServiceDescription.Trim(),
            Deliverables = request.Deliverables.Trim(),
            DevelopmentCost = request.DevelopmentCost,
            AdditionalCost = request.AdditionalCost,
            TaxRate = request.TaxRate,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            AdvancePaymentAmount = advance,
            RemainingPaymentAmount = remaining,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            ValidityDays = request.ValidityDays > 0 ? request.ValidityDays : 15,
            ValidUntil = DateTime.UtcNow.AddDays(request.ValidityDays > 0 ? request.ValidityDays : 15),
            TermsAndConditions = string.IsNullOrWhiteSpace(request.TermsAndConditions)
                ? "1. 40% advance payment required to commence development.\n2. Balance payable upon milestone demonstration and final handover.\n3. Standard warranty & bug fixing support included for 30 days post-delivery."
                : request.TermsAndConditions.Trim(),
            Status = QuotationStatus.Draft,
            SecureToken = Guid.NewGuid().ToString("N")
        };

        foreach (var item in request.Items)
        {
            quotation.Items.Add(new QuotationItem
            {
                Title = item.Title.Trim(),
                Description = item.Description.Trim(),
                Quantity = item.Quantity > 0 ? item.Quantity : 1,
                UnitPrice = item.UnitPrice,
                TotalPrice = (item.Quantity > 0 ? item.Quantity : 1) * item.UnitPrice
            });
        }

        quotation.StatusHistory.Add(new QuotationStatusHistory
        {
            PreviousStatus = null,
            NewStatus = QuotationStatus.Draft,
            ChangedBy = authorName,
            Remarks = "Quotation draft created."
        });

        _context.Quotations.Add(quotation);
        enquiry.Status = EnquiryStatus.Quoted;
        await _context.SaveChangesAsync();

        return MapToDto(quotation, enquiry.ReferenceNumber, enquiry.Customer!);
    }

    public async Task<QuotationDto> UpdateQuotationAsync(int id, UpdateQuotationRequest request, string authorName = "Admin")
    {
        var quotation = await _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Enquiry)
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation == null)
            throw new NotFoundException($"Quotation #{id} not found.");

        if (quotation.Status == QuotationStatus.Accepted)
            throw new BadRequestException("Accepted quotations cannot be modified.");

        var subtotal = request.DevelopmentCost + request.AdditionalCost;
        var taxAmount = Math.Round(subtotal * (request.TaxRate / 100m), 2);
        var totalAmount = subtotal + taxAmount;
        var advance = request.AdvancePaymentAmount > 0 ? request.AdvancePaymentAmount : quotation.AdvancePaymentAmount;
        var remaining = totalAmount - advance;

        quotation.ServiceDescription = request.ServiceDescription.Trim();
        quotation.Deliverables = request.Deliverables.Trim();
        quotation.DevelopmentCost = request.DevelopmentCost;
        quotation.AdditionalCost = request.AdditionalCost;
        quotation.TaxRate = request.TaxRate;
        quotation.TaxAmount = taxAmount;
        quotation.TotalAmount = totalAmount;
        quotation.AdvancePaymentAmount = advance;
        quotation.RemainingPaymentAmount = remaining;
        quotation.ExpectedDeliveryDate = request.ExpectedDeliveryDate;
        quotation.ValidityDays = request.ValidityDays;
        quotation.ValidUntil = quotation.CreatedAt.AddDays(request.ValidityDays);
        quotation.TermsAndConditions = request.TermsAndConditions.Trim();
        quotation.UpdatedAt = DateTime.UtcNow;

        _context.QuotationItems.RemoveRange(quotation.Items);
        quotation.Items.Clear();

        foreach (var item in request.Items)
        {
            quotation.Items.Add(new QuotationItem
            {
                QuotationId = quotation.Id,
                Title = item.Title.Trim(),
                Description = item.Description.Trim(),
                Quantity = item.Quantity > 0 ? item.Quantity : 1,
                UnitPrice = item.UnitPrice,
                TotalPrice = (item.Quantity > 0 ? item.Quantity : 1) * item.UnitPrice
            });
        }

        quotation.StatusHistory.Add(new QuotationStatusHistory
        {
            QuotationId = quotation.Id,
            PreviousStatus = quotation.Status,
            NewStatus = quotation.Status,
            ChangedBy = authorName,
            Remarks = "Quotation details updated."
        });

        await _context.SaveChangesAsync();
        return MapToDto(quotation, quotation.Enquiry!.ReferenceNumber, quotation.Customer!);
    }

    public async Task<QuotationDto> GetQuotationByIdAsync(int id)
    {
        var quotation = await _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Enquiry)
            .Include(q => q.Items)
            .Include(q => q.Project)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation == null)
            throw new NotFoundException($"Quotation #{id} not found.");

        var dto = MapToDto(quotation, quotation.Enquiry!.ReferenceNumber, quotation.Customer!);
        dto.ProjectId = quotation.Project?.Id;
        return dto;
    }

    public async Task<PagedResult<QuotationDto>> GetQuotationsAsync(int page = 1, int pageSize = 10, QuotationStatus? status = null, string? search = null)
    {
        var query = _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Enquiry)
            .Include(q => q.Items)
            .Include(q => q.Project)
            .AsNoTracking();

        if (status.HasValue)
            query = query.Where(q => q.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(q => q.QuotationNumber.ToLower().Contains(s)
                || q.Customer!.FullName.ToLower().Contains(s)
                || q.Customer.Email.ToLower().Contains(s));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(q =>
        {
            var d = MapToDto(q, q.Enquiry?.ReferenceNumber ?? "", q.Customer!);
            d.ProjectId = q.Project?.Id;
            return d;
        }).ToList();

        return new PagedResult<QuotationDto>(dtos, total, page, pageSize);
    }

    public async Task<bool> SendQuotationAsync(int id, string baseQuotationUrl, string authorName = "Admin")
    {
        var quotation = await _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation == null)
            throw new NotFoundException($"Quotation #{id} not found.");

        quotation.Status = QuotationStatus.Sent;
        quotation.SentAt = DateTime.UtcNow;
        quotation.UpdatedAt = DateTime.UtcNow;

        quotation.StatusHistory.Add(new QuotationStatusHistory
        {
            QuotationId = quotation.Id,
            PreviousStatus = QuotationStatus.Draft,
            NewStatus = QuotationStatus.Sent,
            ChangedBy = authorName,
            Remarks = "Quotation PDF generated and sent via email."
        });

        await _context.SaveChangesAsync();

        var secureQuoteLink = $"{baseQuotationUrl.TrimEnd('/')}/quote/{quotation.SecureToken}";
        var settings = await _context.WebsiteSettings.AsNoTracking().ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue);
        var pdfBytes = _pdfGenerator.GenerateQuotationPdf(quotation, quotation.Customer!, quotation.Items, settings);

        await _emailService.SendQuotationEmailAsync(quotation, quotation.Customer!, secureQuoteLink, pdfBytes);
        return true;
    }

    public async Task<PublicQuotationDto> GetPublicQuotationAsync(string secureToken)
    {
        var quotation = await _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.SecureToken == secureToken);

        if (quotation == null)
            throw new NotFoundException("Quotation not found or link has expired.");

        if (quotation.ViewedAt == null && quotation.Status == QuotationStatus.Sent)
        {
            quotation.ViewedAt = DateTime.UtcNow;
            quotation.Status = QuotationStatus.Viewed;
            quotation.StatusHistory.Add(new QuotationStatusHistory
            {
                QuotationId = quotation.Id,
                PreviousStatus = QuotationStatus.Sent,
                NewStatus = QuotationStatus.Viewed,
                ChangedBy = "Customer",
                Remarks = "Customer opened quotation link."
            });
            await _context.SaveChangesAsync();
        }

        return new PublicQuotationDto
        {
            QuotationNumber = quotation.QuotationNumber,
            SecureToken = quotation.SecureToken,
            CustomerName = quotation.Customer!.FullName,
            CompanyName = quotation.Customer.CompanyName,
            ServiceDescription = quotation.ServiceDescription,
            Deliverables = quotation.Deliverables,
            DevelopmentCost = quotation.DevelopmentCost,
            AdditionalCost = quotation.AdditionalCost,
            TaxRate = quotation.TaxRate,
            TaxAmount = quotation.TaxAmount,
            TotalAmount = quotation.TotalAmount,
            AdvancePaymentAmount = quotation.AdvancePaymentAmount,
            RemainingPaymentAmount = quotation.RemainingPaymentAmount,
            ExpectedDeliveryDate = quotation.ExpectedDeliveryDate,
            ValidUntil = quotation.ValidUntil,
            TermsAndConditions = quotation.TermsAndConditions,
            Status = quotation.Status,
            RespondedAt = quotation.RespondedAt,
            ResponseRemarks = quotation.ResponseRemarks,
            Items = quotation.Items.Select(i => new QuotationItemDto
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

    public async Task<bool> CustomerAcceptQuotationAsync(string secureToken, QuotationDecisionRequest request)
    {
        var quotation = await _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Enquiry)
            .Include(q => q.Project)
            .FirstOrDefaultAsync(q => q.SecureToken == secureToken);

        if (quotation == null)
            throw new NotFoundException("Quotation not found.");

        if (quotation.Status == QuotationStatus.Accepted)
            return true; // Already accepted idempotent

        quotation.Status = QuotationStatus.Accepted;
        quotation.RespondedAt = DateTime.UtcNow;
        quotation.ResponseRemarks = request.Remarks?.Trim();
        quotation.UpdatedAt = DateTime.UtcNow;

        if (quotation.Enquiry != null)
        {
            quotation.Enquiry.Status = EnquiryStatus.Converted;
            quotation.Enquiry.UpdatedAt = DateTime.UtcNow;
        }

        quotation.StatusHistory.Add(new QuotationStatusHistory
        {
            QuotationId = quotation.Id,
            PreviousStatus = QuotationStatus.Viewed,
            NewStatus = QuotationStatus.Accepted,
            ChangedBy = "Customer",
            Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? "Accepted by customer." : $"Accepted: {request.Remarks}"
        });

        // Automatically create project if not already created
        if (quotation.Project == null)
        {
            var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
            var countToday = await _context.Projects.CountAsync(p => p.ProjectNumber.StartsWith($"PRJ-{datePrefix}"));
            var projNumber = $"PRJ-{datePrefix}-{(countToday + 1):D4}";

            var project = new Project
            {
                ProjectNumber = projNumber,
                QuotationId = quotation.Id,
                CustomerId = quotation.CustomerId,
                ProjectTitle = quotation.Enquiry?.ProjectTitle ?? quotation.ServiceDescription,
                Description = quotation.Deliverables,
                StartDate = DateTime.UtcNow,
                ExpectedCompletionDate = quotation.ExpectedDeliveryDate,
                AgreedAmount = quotation.TotalAmount,
                Status = ProjectStatus.InProgress,
                ProgressPercentage = 10,
                InternalNotes = $"Automatically generated upon quotation {quotation.QuotationNumber} acceptance."
            };

            // Add standard initial milestones
            project.Milestones.Add(new ProjectMilestone
            {
                Title = "Project Kickoff & Architecture Setup",
                Description = "Initial setup, environment configuration, database design and sprint planning.",
                DueDate = DateTime.UtcNow.AddDays(3),
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow,
                Amount = quotation.AdvancePaymentAmount
            });
            project.Milestones.Add(new ProjectMilestone
            {
                Title = "Core Development & Milestone Review",
                Description = "Implementation of primary modules, APIs, and frontend integration.",
                DueDate = quotation.ExpectedDeliveryDate.AddDays(-5),
                IsCompleted = false,
                Amount = quotation.RemainingPaymentAmount / 2
            });
            project.Milestones.Add(new ProjectMilestone
            {
                Title = "Final Deployment, Handover & Viva/Docs",
                Description = "Testing, code explanation, documentation and final handover.",
                DueDate = quotation.ExpectedDeliveryDate,
                IsCompleted = false,
                Amount = quotation.RemainingPaymentAmount / 2
            });

            _context.Projects.Add(project);
        }

        await _context.SaveChangesAsync();
        await _emailService.SendQuotationAcceptedNotificationAsync(quotation, quotation.Customer!);
        return true;
    }

    public async Task<bool> CustomerRejectQuotationAsync(string secureToken, QuotationDecisionRequest request)
    {
        var quotation = await _context.Quotations
            .Include(q => q.Customer)
            .FirstOrDefaultAsync(q => q.SecureToken == secureToken);

        if (quotation == null)
            throw new NotFoundException("Quotation not found.");

        quotation.Status = QuotationStatus.Rejected;
        quotation.RespondedAt = DateTime.UtcNow;
        quotation.ResponseRemarks = request.Remarks?.Trim();
        quotation.UpdatedAt = DateTime.UtcNow;

        quotation.StatusHistory.Add(new QuotationStatusHistory
        {
            QuotationId = quotation.Id,
            PreviousStatus = QuotationStatus.Viewed,
            NewStatus = QuotationStatus.Rejected,
            ChangedBy = "Customer",
            Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? "Rejected by customer." : $"Rejected: {request.Remarks}"
        });

        await _context.SaveChangesAsync();
        await _emailService.SendQuotationRejectedNotificationAsync(quotation, quotation.Customer!);
        return true;
    }

    public async Task<byte[]> DownloadQuotationPdfAsync(int id)
    {
        var quotation = await _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation == null)
            throw new NotFoundException($"Quotation #{id} not found.");

        var settings = await _context.WebsiteSettings.AsNoTracking().ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue);
        return _pdfGenerator.GenerateQuotationPdf(quotation, quotation.Customer!, quotation.Items, settings);
    }

    private static QuotationDto MapToDto(Quotation q, string enquiryRef, Customer c)
    {
        return new QuotationDto
        {
            Id = q.Id,
            QuotationNumber = q.QuotationNumber,
            EnquiryId = q.EnquiryId,
            EnquiryReferenceNumber = enquiryRef,
            CustomerId = q.CustomerId,
            CustomerName = c.FullName,
            CustomerEmail = c.Email,
            CustomerPhone = c.PhoneNumber,
            CompanyName = c.CompanyName,
            ServiceDescription = q.ServiceDescription,
            Deliverables = q.Deliverables,
            DevelopmentCost = q.DevelopmentCost,
            AdditionalCost = q.AdditionalCost,
            TaxRate = q.TaxRate,
            TaxAmount = q.TaxAmount,
            TotalAmount = q.TotalAmount,
            AdvancePaymentAmount = q.AdvancePaymentAmount,
            RemainingPaymentAmount = q.RemainingPaymentAmount,
            ExpectedDeliveryDate = q.ExpectedDeliveryDate,
            ValidityDays = q.ValidityDays,
            ValidUntil = q.ValidUntil,
            TermsAndConditions = q.TermsAndConditions,
            Status = q.Status,
            SecureToken = q.SecureToken,
            SentAt = q.SentAt,
            ViewedAt = q.ViewedAt,
            RespondedAt = q.RespondedAt,
            ResponseRemarks = q.ResponseRemarks,
            CreatedAt = q.CreatedAt,
            Items = q.Items.Select(i => new QuotationItemDto
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
}
