using AffordStack.Domain.Common;

namespace AffordStack.Domain.Entities;

public class WebsiteSetting : BaseEntity
{
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string SettingGroup { get; set; } = "General"; // "General", "Contact", "Social", "Email"
    public string? Description { get; set; }
}
