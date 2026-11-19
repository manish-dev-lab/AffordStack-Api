using SoftwareServices.Domain.Common;

namespace SoftwareServices.Domain.Entities;

public class EnquiryAttachment : BaseEntity
{
    public int EnquiryId { get; set; }
    public Enquiry? Enquiry { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
}
