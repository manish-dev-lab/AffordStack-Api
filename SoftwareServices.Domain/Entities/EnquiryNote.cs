using SoftwareServices.Domain.Common;

namespace SoftwareServices.Domain.Entities;

public class EnquiryNote : BaseEntity
{
    public int EnquiryId { get; set; }
    public Enquiry? Enquiry { get; set; }

    public int? AdminUserId { get; set; }
    public AdminUser? AdminUser { get; set; }

    public string NoteContent { get; set; } = string.Empty;
}
