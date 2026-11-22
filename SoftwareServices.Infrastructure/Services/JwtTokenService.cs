using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SoftwareServices.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(AdminUser user, int timeoutMinutes = 0)
    {
        var secret = _configuration["Jwt:Secret"] ?? "SoftwareServices_SuperSecret_Jwt_Security_Key_2026_Minimum_32_Chars_Long!!";
        var issuer = _configuration["Jwt:Issuer"] ?? "AffordStackPlatform";
        var audience = _configuration["Jwt:Audience"] ?? "AffordStackPlatformUsers";
        
        DateTime expiresAt = timeoutMinutes > 0
            ? DateTime.UtcNow.AddMinutes(timeoutMinutes)
            : DateTime.UtcNow.AddDays(int.TryParse(_configuration["Jwt:ExpiryDays"], out var days) ? days : 7);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("user_type", "admin")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateUserToken(User user, int timeoutMinutes = 0)
    {
        var secret = _configuration["Jwt:Secret"] ?? "SoftwareServices_SuperSecret_Jwt_Security_Key_2026_Minimum_32_Chars_Long!!";
        var issuer = _configuration["Jwt:Issuer"] ?? "AffordStackPlatform";
        var audience = _configuration["Jwt:Audience"] ?? "AffordStackPlatformUsers";

        DateTime expiresAt = timeoutMinutes > 0
            ? DateTime.UtcNow.AddMinutes(timeoutMinutes)
            : DateTime.UtcNow.AddDays(int.TryParse(_configuration["Jwt:ExpiryDays"], out var days) ? days : 7);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.MobilePhone, user.PhoneNumber),
            new(ClaimTypes.Role, "Customer"),
            new("user_type", "customer")
        };

        if (user.CustomerId.HasValue)
        {
            claims.Add(new Claim("customer_id", user.CustomerId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
