using AffordStack.Domain.Common;

namespace AffordStack.Domain.Entities;

public class Customer : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CompanyName { get; set; }

    public User? User { get; set; }

    public ICollection<Enquiry> Enquiries { get; set; } = new List<Enquiry>();
    public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
