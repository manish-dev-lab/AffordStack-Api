using Microsoft.EntityFrameworkCore;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.Common.Exceptions;
using SoftwareServices.Application.DTOs.Payment;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Domain.Entities;
using SoftwareServices.Domain.Enums;

namespace SoftwareServices.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IApplicationDbContext _context;

    public PaymentService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentDto> RecordPaymentAsync(RecordPaymentRequest request)
    {
        var project = await _context.Projects
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId);

        if (project == null)
            throw new NotFoundException($"Project #{request.ProjectId} not found.");

        var payment = new Payment
        {
            ProjectId = project.Id,
            CustomerId = project.CustomerId,
            Customer = project.Customer,
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
            ProjectId = project.Id,
            ProjectNumber = project.ProjectNumber,
            ProjectTitle = project.ProjectTitle,
            CustomerId = project.CustomerId,
            CustomerName = project.Customer?.FullName ?? "",
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
            .Include(p => p.Customer)
            .AsNoTracking();

        if (projectId.HasValue)
            query = query.Where(p => p.ProjectId == projectId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.TransactionReference != null && p.TransactionReference.ToLower().Contains(s)
                || p.Customer!.FullName.ToLower().Contains(s)
                || p.Project!.ProjectNumber.ToLower().Contains(s));
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
                ProjectNumber = p.Project!.ProjectNumber,
                ProjectTitle = p.Project.ProjectTitle,
                CustomerId = p.CustomerId,
                CustomerName = p.Customer!.FullName,
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
}
