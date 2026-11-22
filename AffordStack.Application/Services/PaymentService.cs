using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AffordStack.Application.Common;
using AffordStack.Application.Common.Exceptions;
using AffordStack.Application.DTOs.Payment;
using AffordStack.Application.Interfaces;
using AffordStack.Domain.Entities;
using AffordStack.Domain.Enums;

namespace AffordStack.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IApplicationDbContext _context;
    private readonly ICashfreeService _cashfreeService;
    private readonly IEmailService _emailService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IApplicationDbContext context,
        ICashfreeService cashfreeService,
        IEmailService emailService,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _cashfreeService = cashfreeService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<PaymentDto> RecordPaymentAsync(RecordPaymentRequest request)
    {
        Project? project = null;
        Customer? customer = null;

        if (request.ProjectId.HasValue)
        {
            project = await _context.Projects
                .Include(p => p.Customer)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId.Value);

            if (project == null)
                throw new NotFoundException($"Project #{request.ProjectId.Value} not found.");

            customer = project.Customer;
        }

        Quotation? quotation = null;
        if (request.QuotationId.HasValue)
        {
            quotation = await _context.Quotations
                .Include(q => q.Customer)
                .FirstOrDefaultAsync(q => q.Id == request.QuotationId.Value);

            if (quotation == null)
                throw new NotFoundException($"Quotation #{request.QuotationId.Value} not found.");

            customer ??= quotation.Customer;
        }

        if (customer == null)
            throw new BadRequestException("Payment must be associated with either an existing project or quotation.");

        var payment = new Payment
        {
            ProjectId = project?.Id,
            CustomerId = customer.Id,
            Customer = customer,
            QuotationId = quotation?.Id,
            PaymentType = request.PaymentType,
            PaymentDate = request.PaymentDate,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod.Trim(),
            TransactionReference = request.TransactionReference?.Trim(),
            Status = request.Status,
            Notes = request.Notes?.Trim()
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return new PaymentDto
        {
            Id = payment.Id,
            ProjectId = project?.Id,
            ProjectNumber = project?.ProjectNumber,
            ProjectTitle = project?.ProjectTitle,
            CustomerId = customer.Id,
            CustomerName = customer.FullName,
            QuotationId = quotation?.Id,
            QuotationNumber = quotation?.QuotationNumber,
            PaymentType = payment.PaymentType,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            TransactionReference = payment.TransactionReference,
            Status = payment.Status,
            Notes = payment.Notes,
            CreatedAt = payment.CreatedAt
        };
    }

    public async Task<PagedResult<PaymentDto>> GetPaymentsAsync(int page = 1, int pageSize = 10, int? projectId = null, string? search = null)
    {
        var query = _context.Payments
            .Include(p => p.Project)
            .Include(p => p.Quotation)
            .Include(p => p.Customer)
            .AsNoTracking();

        if (projectId.HasValue)
            query = query.Where(p => p.ProjectId == projectId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p =>
                (p.TransactionReference != null && p.TransactionReference.ToLower().Contains(s))
                || (p.CashfreeOrderId != null && p.CashfreeOrderId.ToLower().Contains(s))
                || (p.Customer != null && p.Customer.FullName.ToLower().Contains(s))
                || (p.Project != null && p.Project.ProjectNumber.ToLower().Contains(s)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                ProjectId = p.ProjectId,
                ProjectNumber = p.Project != null ? p.Project.ProjectNumber : null,
                ProjectTitle = p.Project != null ? p.Project.ProjectTitle : null,
                CustomerId = p.CustomerId,
                CustomerName = p.Customer != null ? p.Customer.FullName : "",
                QuotationId = p.QuotationId,
                QuotationNumber = p.Quotation != null ? p.Quotation.QuotationNumber : null,
                CashfreeOrderId = p.CashfreeOrderId,
                CashfreePaymentId = p.CashfreePaymentId,
                PaymentType = p.PaymentType,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                TransactionReference = p.TransactionReference,
                Status = p.Status,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<PaymentDto>(items, total, page, pageSize);
    }

    public async Task<CashfreeOrderResponse> CreateQuotationAdvanceOrderAsync(string secureToken, string? returnUrl = null)
    {
        var quotation = await _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Enquiry)
            .FirstOrDefaultAsync(q => q.SecureToken == secureToken);

        if (quotation == null)
            throw new NotFoundException("Quotation proposal not found.");

        if (quotation.IsAdvancePaid)
            throw new BadRequestException("Advance payment for this quotation has already been received.");

        if (quotation.AdvancePaymentAmount <= 0)
            throw new BadRequestException("This quotation does not require an advance payment.");

        var customer = quotation.Customer
            ?? throw new BadRequestException("Customer record missing for this quotation.");

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // Keep order_id <= 45 chars for Cashfree
        var orderId = $"CF_Q_{quotation.Id}_{timestamp}";
        var orderNote = $"Advance payment for quotation #{quotation.QuotationNumber} ({customer.FullName})";

        return await _cashfreeService.CreateOrderAsync(
            orderId: orderId,
            amount: quotation.AdvancePaymentAmount,
            customerId: $"cust_{customer.Id}",
            customerName: customer.FullName,
            customerEmail: customer.Email,
            customerPhone: customer.PhoneNumber,
            orderNote: orderNote,
            returnUrl: returnUrl);
    }

    public async Task<CashfreeOrderResponse> CreateMilestoneOrderAsync(int milestoneId, int customerId, string? returnUrl = null)
    {
        var milestone = await _context.ProjectMilestones
            .Include(m => m.Project)
                .ThenInclude(p => p!.Customer)
            .FirstOrDefaultAsync(m => m.Id == milestoneId);

        if (milestone == null || milestone.Project == null)
            throw new NotFoundException($"Project milestone #{milestoneId} not found.");

        if (milestone.Project.CustomerId != customerId)
            throw new UnauthorizedException("You are not authorized to pay for this milestone.");

        if (milestone.IsPaid)
            throw new BadRequestException("This milestone has already been paid.");

        if (milestone.Amount <= 0)
            throw new BadRequestException("Milestone amount must be greater than zero.");

        var customer = milestone.Project.Customer
            ?? throw new BadRequestException("Customer information is missing for this project.");

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var orderId = $"CF_M_{milestone.Id}_{timestamp}";
        var orderNote = $"Milestone payment for '{milestone.Title}' - Project {milestone.Project.ProjectNumber}";

        // Save reference ahead of time for fast lookup
        milestone.PaymentReference = orderId;
        await _context.SaveChangesAsync();

        return await _cashfreeService.CreateOrderAsync(
            orderId: orderId,
            amount: milestone.Amount,
            customerId: $"cust_{customer.Id}",
            customerName: customer.FullName,
            customerEmail: customer.Email,
            customerPhone: customer.PhoneNumber,
            orderNote: orderNote,
            returnUrl: returnUrl);
    }

    public async Task<PaymentVerificationResponse> VerifyAndCompletePaymentAsync(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw new BadRequestException("Order ID is required.");

        var verification = await _cashfreeService.GetOrderDetailsAsync(orderId);

        if (!verification.Success)
        {
            return verification;
        }

        // Check if already recorded (idempotent protection)
        var existingPayment = await _context.Payments
            .FirstOrDefaultAsync(p => p.CashfreeOrderId == orderId);

        if (existingPayment != null)
        {
            return new PaymentVerificationResponse
            {
                Success = true,
                OrderId = orderId,
                PaymentId = existingPayment.CashfreePaymentId ?? verification.PaymentId,
                Status = "PAID",
                Amount = existingPayment.Amount,
                PaidAt = existingPayment.PaymentDate,
                Message = "Payment already confirmed and processed."
            };
        }

        // Case 1: Quotation Advance Payment (orderId contains CF_Q_ or quotation lookup)
        if (orderId.StartsWith("CF_Q_") || orderId.Contains("_Q_"))
        {
            var parts = orderId.Split('_');
            int quoteId = 0;
            if (parts.Length >= 3 && int.TryParse(parts[2], out int parsedId))
            {
                quoteId = parsedId;
            }

            var quotation = await _context.Quotations
                .Include(q => q.Customer)
                .Include(q => q.Enquiry)
                .Include(q => q.Project)
                    .ThenInclude(p => p!.Milestones)
                .FirstOrDefaultAsync(q => q.Id == quoteId);

            if (quotation != null)
            {
                quotation.IsAdvancePaid = true;
                quotation.AdvancePaidAt = verification.PaidAt ?? DateTime.UtcNow;

                // If quotation was not yet accepted, accept it now automatically upon payment
                if (quotation.Status != QuotationStatus.Accepted)
                {
                    quotation.Status = QuotationStatus.Accepted;
                    quotation.RespondedAt = DateTime.UtcNow;
                    quotation.ResponseRemarks = $"Accepted via Cashfree Advance Payment (Order: {orderId})";
                    quotation.UpdatedAt = DateTime.UtcNow;

                    if (quotation.Enquiry != null)
                    {
                        quotation.Enquiry.Status = EnquiryStatus.Converted;
                        quotation.Enquiry.UpdatedAt = DateTime.UtcNow;
                    }

                    // Create Project if not yet created
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
                            ProgressPercentage = 15,
                            InternalNotes = $"Auto-provisioned upon receipt of advance payment #{orderId}."
                        };

                        // Add kickoff milestone (Mark as Paid)
                        project.Milestones.Add(new ProjectMilestone
                        {
                            Title = "Project Kickoff & Architecture Setup",
                            Description = "Initial setup, environment configuration, database design and sprint planning.",
                            DueDate = DateTime.UtcNow.AddDays(3),
                            IsCompleted = true,
                            CompletedAt = DateTime.UtcNow,
                            Amount = quotation.AdvancePaymentAmount,
                            IsPaid = true,
                            PaidAt = verification.PaidAt ?? DateTime.UtcNow,
                            PaymentReference = orderId
                        });

                        project.Milestones.Add(new ProjectMilestone
                        {
                            Title = "Core Development & Milestone Review",
                            Description = "Implementation of primary modules, APIs, and frontend integration.",
                            DueDate = quotation.ExpectedDeliveryDate.AddDays(-5),
                            IsCompleted = false,
                            Amount = quotation.RemainingPaymentAmount / 2,
                            IsPaid = false
                        });

                        project.Milestones.Add(new ProjectMilestone
                        {
                            Title = "Final Deployment, Handover & Viva/Docs",
                            Description = "Testing, code explanation, documentation and final handover.",
                            DueDate = quotation.ExpectedDeliveryDate,
                            IsCompleted = false,
                            Amount = quotation.RemainingPaymentAmount / 2,
                            IsPaid = false
                        });

                        _context.Projects.Add(project);
                        quotation.Project = project;
                    }
                }
                else if (quotation.Project != null)
                {
                    // If project already exists, mark kickoff milestone as paid
                    var kickoff = quotation.Project.Milestones.FirstOrDefault(m => m.Title.Contains("Kickoff") || m.Amount == quotation.AdvancePaymentAmount);
                    if (kickoff != null)
                    {
                        kickoff.IsPaid = true;
                        kickoff.PaidAt = verification.PaidAt ?? DateTime.UtcNow;
                        kickoff.PaymentReference = orderId;
                    }
                }

                // Insert Payment Record
                var payment = new Payment
                {
                    ProjectId = quotation.Project?.Id,
                    QuotationId = quotation.Id,
                    CustomerId = quotation.CustomerId,
                    PaymentType = PaymentType.Advance,
                    PaymentDate = verification.PaidAt ?? DateTime.UtcNow,
                    Amount = verification.Amount > 0 ? verification.Amount : quotation.AdvancePaymentAmount,
                    PaymentMethod = "Cashfree PG",
                    TransactionReference = verification.PaymentId ?? orderId,
                    CashfreeOrderId = orderId,
                    CashfreePaymentId = verification.PaymentId,
                    Status = PaymentStatus.Received,
                    Notes = $"Advance payment for quotation #{quotation.QuotationNumber} captured via Cashfree"
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully recorded advance payment for Quotation #{QuotationNumber} via Cashfree Order {OrderId}", quotation.QuotationNumber, orderId);

                return new PaymentVerificationResponse
                {
                    Success = true,
                    OrderId = orderId,
                    PaymentId = verification.PaymentId,
                    Status = "PAID",
                    Amount = payment.Amount,
                    PaidAt = payment.PaymentDate,
                    Message = "Advance payment received successfully. Project kickoff initiated!"
                };
            }
        }

        // Case 2: Milestone Payment (orderId contains CF_M_ or payment reference)
        var milestone = await _context.ProjectMilestones
            .Include(m => m.Project)
                .ThenInclude(p => p!.Customer)
            .FirstOrDefaultAsync(m => m.PaymentReference == orderId);

        if (milestone == null && orderId.StartsWith("CF_M_"))
        {
            var parts = orderId.Split('_');
            if (parts.Length >= 3 && int.TryParse(parts[2], out int mId))
            {
                milestone = await _context.ProjectMilestones
                    .Include(m => m.Project)
                        .ThenInclude(p => p!.Customer)
                    .FirstOrDefaultAsync(m => m.Id == mId);
            }
        }

        if (milestone != null && milestone.Project != null)
        {
            milestone.IsPaid = true;
            milestone.PaidAt = verification.PaidAt ?? DateTime.UtcNow;
            milestone.PaymentReference = orderId;

            var payment = new Payment
            {
                ProjectId = milestone.ProjectId,
                CustomerId = milestone.Project.CustomerId,
                PaymentType = PaymentType.Milestone,
                PaymentDate = verification.PaidAt ?? DateTime.UtcNow,
                Amount = verification.Amount > 0 ? verification.Amount : milestone.Amount,
                PaymentMethod = "Cashfree PG",
                TransactionReference = verification.PaymentId ?? orderId,
                CashfreeOrderId = orderId,
                CashfreePaymentId = verification.PaymentId,
                Status = PaymentStatus.Received,
                Notes = $"Milestone payment for '{milestone.Title}' (Project {milestone.Project.ProjectNumber}) captured via Cashfree"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully recorded milestone payment for Milestone '{Title}' via Cashfree Order {OrderId}", milestone.Title, orderId);

            return new PaymentVerificationResponse
            {
                Success = true,
                OrderId = orderId,
                PaymentId = verification.PaymentId,
                Status = "PAID",
                Amount = payment.Amount,
                PaidAt = payment.PaymentDate,
                Message = $"Milestone payment for '{milestone.Title}' received successfully!"
            };
        }

        return verification;
    }

    public async Task<List<CustomerPaymentDto>> GetCustomerPaymentsAsync(int customerId)
    {
        return await _context.Payments
            .Include(p => p.Project)
            .Include(p => p.Quotation)
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new CustomerPaymentDto
            {
                Id = p.Id,
                ProjectId = p.ProjectId,
                ProjectNumber = p.Project != null ? p.Project.ProjectNumber : null,
                ProjectTitle = p.Project != null ? p.Project.ProjectTitle : null,
                QuotationId = p.QuotationId,
                QuotationNumber = p.Quotation != null ? p.Quotation.QuotationNumber : null,
                PaymentType = p.PaymentType,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                TransactionReference = p.TransactionReference,
                CashfreeOrderId = p.CashfreeOrderId,
                CashfreePaymentId = p.CashfreePaymentId,
                Status = p.Status,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }
}
