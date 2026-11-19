using SoftwareServices.Domain.Common;
using SoftwareServices.Domain.Enums;

namespace SoftwareServices.Domain.Entities;

public class AdminUser : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public AdminRole Role { get; set; } = AdminRole.Admin;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
}
