namespace SoftwareServices.Application.DTOs.Settings;

public class WebsiteSettingDto
{
    public int Id { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string SettingGroup { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateSettingRequest
{
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
}
