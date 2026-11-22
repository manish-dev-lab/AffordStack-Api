using AffordStack.Domain.Common;

namespace AffordStack.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public ICollection<Enquiry> Enquiries { get; set; } = new List<Enquiry>();
}
