using AffordStack.Domain.Common;
using AffordStack.Domain.Enums;

namespace AffordStack.Domain.Entities;

public class Payment : BaseEntity
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public PaymentType PaymentType { get; set; } = PaymentType.Advance;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Bank Transfer";
    public string? TransactionReference { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Received;
    public string? Notes { get; set; }
}
