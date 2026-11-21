using SoftwareServices.Application.DTOs.Configuration;

namespace SoftwareServices.Application.Interfaces;

public interface IConfigurationService
{
    Task<TokenConfigDto> GetTokenConfigAsync();
    Task<EmailConfigDto> GetEmailConfigAsync();
    Task<WebsiteConfigDto> GetWebsiteConfigAsync();
    Task<List<AppConfigItemDto>> GetAllConfigurationsAsync();
    Task<string?> GetRawConfigAsync(string key);
    Task<bool> UpdateConfigurationAsync(string key, string jsonValue, string? description = null);
    Task<bool> UpdateConfigurationsBatchAsync(Dictionary<string, string> configs);
}
