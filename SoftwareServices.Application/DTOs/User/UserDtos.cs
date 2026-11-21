using SoftwareServices.Domain.Enums;

namespace SoftwareServices.Application.DTOs.User;

public class UserRegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? Otp { get; set; }
}

public class SendRegistrationOtpRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class UserLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserProfileDto User { get; set; } = new();
}

public class UserProfileDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public int TotalEnquiries { get; set; }
    public int ActiveQuotations { get; set; }
    public int ActiveProjects { get; set; }
}

public class UserEnquirySummaryDto
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string ProjectTitle { get; set; } = string.Empty;
    public string ProjectDescription { get; set; } = string.Empty;
    public string RequiredTechnology { get; set; } = string.Empty;
    public string EstimatedBudget { get; set; } = string.Empty;
    public EnquiryStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Linked quotation details if available
    public string? QuotationNumber { get; set; }
    public string? QuotationSecureToken { get; set; }
    public decimal? QuotationAmount { get; set; }
    public QuotationStatus? QuotationStatus { get; set; }
}

public class UserChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class AdminUserAccountDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int EnquiriesCount { get; set; }
    public int QuotationsCount { get; set; }
}
