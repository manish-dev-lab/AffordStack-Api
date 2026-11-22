using AffordStack.Domain.Common;
using AffordStack.Domain.Enums;

namespace AffordStack.Domain.Entities;

public class QuotationStatusHistory : BaseEntity
{
    public int QuotationId { get; set; }
    public Quotation? Quotation { get; set; }

    public QuotationStatus? PreviousStatus { get; set; }
    public QuotationStatus NewStatus { get; set; }
    public string ChangedBy { get; set; } = "System";
    public string? Remarks { get; set; }
}
