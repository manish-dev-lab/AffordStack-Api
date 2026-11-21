using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SoftwareServices.Application.DTOs.Configuration;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Domain.Entities;
using System.Text.Json;

namespace SoftwareServices.Application.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly IApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConfigurationService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private const string CachePrefix = "cfg:";
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromHours(1);

    public ConfigurationService(
        IApplicationDbContext context,
        IMemoryCache cache,
        ILogger<ConfigurationService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<TokenConfigDto> GetTokenConfigAsync()
    {
        const string key = "TokenConfig";
        var cacheKey = $"{CachePrefix}{key}";

        if (_cache.TryGetValue(cacheKey, out TokenConfigDto? cached) && cached != null)
        {
            return cached;
        }

        var config = await _context.AppConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConfigKey == key);

        TokenConfigDto result = new();
        if (config != null && !string.IsNullOrWhiteSpace(config.ConfigValue))
        {
            try
            {
                result = JsonSerializer.Deserialize<TokenConfigDto>(config.ConfigValue, JsonOptions) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing TokenConfig from database. Using defaults.");
            }
        }

        _cache.Set(cacheKey, result, DefaultCacheDuration);
        return result;
    }

    public async Task<EmailConfigDto> GetEmailConfigAsync()
    {
        const string key = "EmailConfig";
        var cacheKey = $"{CachePrefix}{key}";

        if (_cache.TryGetValue(cacheKey, out EmailConfigDto? cached) && cached != null)
        {
            return cached;
        }

        var config = await _context.AppConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConfigKey == key);

        EmailConfigDto result = new();
        if (config != null && !string.IsNullOrWhiteSpace(config.ConfigValue))
        {
            try
            {
                result = JsonSerializer.Deserialize<EmailConfigDto>(config.ConfigValue, JsonOptions) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing EmailConfig from database. Using defaults.");
            }
        }

        _cache.Set(cacheKey, result, DefaultCacheDuration);
        return result;
    }

    public async Task<WebsiteConfigDto> GetWebsiteConfigAsync()
    {
        const string key = "WebsiteConfig";
        var cacheKey = $"{CachePrefix}{key}";

        if (_cache.TryGetValue(cacheKey, out WebsiteConfigDto? cached) && cached != null)
        {
            return cached;
        }

        var config = await _context.AppConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConfigKey == key);

        WebsiteConfigDto result = new();
        if (config != null && !string.IsNullOrWhiteSpace(config.ConfigValue))
        {
            try
            {
                result = JsonSerializer.Deserialize<WebsiteConfigDto>(config.ConfigValue, JsonOptions) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing WebsiteConfig from database. Using defaults.");
            }
        }

        _cache.Set(cacheKey, result, DefaultCacheDuration);
        return result;
    }

    public async Task<List<AppConfigItemDto>> GetAllConfigurationsAsync()
    {
        var configs = await _context.AppConfigurations
            .AsNoTracking()
            .OrderBy(c => c.ConfigKey)
            .Select(c => new AppConfigItemDto
            {
                Key = c.ConfigKey,
                Value = c.ConfigValue,
                Description = c.Description,
                UpdatedAt = c.UpdatedAt ?? c.CreatedAt
            })
            .ToListAsync();

        return configs;
    }

    public async Task<string?> GetRawConfigAsync(string key)
    {
        var cacheKey = $"{CachePrefix}{key}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
        {
            return cached;
        }

        var config = await _context.AppConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConfigKey == key);

        if (config == null) return null;

        _cache.Set(cacheKey, config.ConfigValue, DefaultCacheDuration);
        return config.ConfigValue;
    }

    public async Task<bool> UpdateConfigurationAsync(string key, string jsonValue, string? description = null)
    {
        var config = await _context.AppConfigurations.FirstOrDefaultAsync(c => c.ConfigKey == key);
        if (config == null)
        {
            config = new AppConfiguration
            {
                ConfigKey = key,
                ConfigValue = jsonValue,
                Description = description ?? $"Configuration for {key}"
            };
            _context.AppConfigurations.Add(config);
        }
        else
        {
            config.ConfigValue = jsonValue;
            if (!string.IsNullOrWhiteSpace(description))
            {
                config.Description = description;
            }
            config.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Invalidate cache immediately so changes reflect everywhere
        _cache.Remove($"{CachePrefix}{key}");
        return true;
    }

    public async Task<bool> UpdateConfigurationsBatchAsync(Dictionary<string, string> configs)
    {
        foreach (var (key, jsonValue) in configs)
        {
            var config = await _context.AppConfigurations.FirstOrDefaultAsync(c => c.ConfigKey == key);
            if (config == null)
            {
                config = new AppConfiguration
                {
                    ConfigKey = key,
                    ConfigValue = jsonValue,
                    Description = $"Configuration for {key}"
                };
                _context.AppConfigurations.Add(config);
            }
            else
            {
                config.ConfigValue = jsonValue;
                config.UpdatedAt = DateTime.UtcNow;
            }

            _cache.Remove($"{CachePrefix}{key}");
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
