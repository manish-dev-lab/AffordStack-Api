namespace AffordStack.Application.DTOs.Configuration;

public class TokenConfigDto
{
    public int TokenTimeoutMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 7;
}

public class EmailConfigDto
{
    public bool EmailsEnabled { get; set; } = true;
    public bool SendCustomerNotifications { get; set; } = true;
    public bool SendAdminNotifications { get; set; } = true;
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = "softwareservices2001@gmail.com";
    public string Password { get; set; } = "niywncoibgfffxbd";
    public string FromEmail { get; set; } = "softwareservices2001@gmail.com";
    public string FromName { get; set; } = "AffordStack";
    public string AdminNotificationEmail { get; set; } = "softwareservices2001@gmail.com";
    public bool EnableSsl { get; set; } = true;
}

public class WebsiteConfigDto
{
    public string SiteName { get; set; } = "AffordStack Platform";
    public string SupportEmail { get; set; } = "manishkiran8464@gmail.com";
    public string SupportPhone { get; set; } = "+91 8464992957";
    public string ContactAddress { get; set; } = "Rc puram Hyd";
    public bool MaintenanceMode { get; set; } = false;
}

public class AppConfigItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdateConfigValueRequest
{
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
