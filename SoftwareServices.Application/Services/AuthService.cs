using Microsoft.EntityFrameworkCore;
using SoftwareServices.Application.Common.Exceptions;
using SoftwareServices.Application.DTOs.Auth;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Domain.Entities;

namespace SoftwareServices.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(IApplicationDbContext context, IJwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var usernameOrEmail = request.UsernameOrEmail.Trim().ToLower();
        var user = await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Username.ToLower() == usernameOrEmail || u.Email.ToLower() == usernameOrEmail);

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedException("Invalid username or password.");
        }

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedException("Invalid username or password.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtTokenService.GenerateToken(user);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            User = new AdminUserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                LastLoginAt = user.LastLoginAt
            }
        };
    }

    public async Task<bool> ChangePasswordAsync(int adminUserId, ChangePasswordRequest request)
    {
        var user = await _context.AdminUsers.FindAsync(adminUserId);
        if (user == null)
            throw new NotFoundException("Admin user not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BadRequestException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AdminUserDto> GetProfileAsync(int adminUserId)
    {
        var user = await _context.AdminUsers.FindAsync(adminUserId);
        if (user == null)
            throw new NotFoundException("Admin user not found.");

        return new AdminUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            LastLoginAt = user.LastLoginAt
        };
    }
}
