using AffordStack.Domain.Common;

namespace AffordStack.Domain.Entities;

public class AppConfiguration : BaseEntity
{
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty; // JSON-formatted configuration value
    public string? Description { get; set; }
}
