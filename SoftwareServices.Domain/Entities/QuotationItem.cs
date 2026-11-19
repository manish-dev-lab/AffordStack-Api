using SoftwareServices.Domain.Common;

namespace SoftwareServices.Domain.Entities;

public class QuotationItem : BaseEntity
{
    public int QuotationId { get; set; }
    public Quotation? Quotation { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
