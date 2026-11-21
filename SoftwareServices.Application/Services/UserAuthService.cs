using Microsoft.EntityFrameworkCore;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.Common.Exceptions;
using SoftwareServices.Application.DTOs.User;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Domain.Entities;
using SoftwareServices.Domain.Enums;

namespace SoftwareServices.Application.Services;

public class UserAuthService : IUserAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfigurationService _configurationService;

    public UserAuthService(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IConfigurationService configurationService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _configurationService = configurationService;
    }

    public async Task<UserLoginResponse> RegisterAsync(UserRegisterRequest request)
    {
        var email = request.Email.Trim().ToLower();

        var existingUser = await _context.Users.AnyAsync(u => u.Email.ToLower() == email);
        if (existingUser)
        {
            throw new BadRequestException("An account with this email address already exists. Please sign in instead.");
        }

        // Check if there is an existing Customer record from past guest enquiries
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == email);
        if (customer == null)
        {
            customer = new Customer
            {
                FullName = request.FullName.Trim(),
                Email = email,
                PhoneNumber = request.PhoneNumber.Trim(),
                CompanyName = request.CompanyName?.Trim()
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = request.PhoneNumber.Trim(),
            CompanyName = request.CompanyName?.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CustomerId = customer.Id,
            IsActive = true,
            LastLoginAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Associate any existing past enquiries for this email with the user
        var pastEnquiries = await _context.Enquiries
            .Where(e => e.CustomerId == customer.Id && e.UserId == null)
            .ToListAsync();

        foreach (var enq in pastEnquiries)
        {
            enq.UserId = user.Id;
        }

        if (pastEnquiries.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        var tokenConfig = await _configurationService.GetTokenConfigAsync();
        var token = _jwtTokenService.GenerateUserToken(user, tokenConfig.TokenTimeoutMinutes);
        var expiresAt = tokenConfig.TokenTimeoutMinutes > 0
            ? DateTime.UtcNow.AddMinutes(tokenConfig.TokenTimeoutMinutes)
            : DateTime.UtcNow.AddDays(7);

        return new UserLoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CompanyName = user.CompanyName,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                TotalEnquiries = pastEnquiries.Count
            }
        };
    }

    public async Task<UserLoginResponse> LoginAsync(UserLoginRequest request)
    {
        var email = request.Email.Trim().ToLower();
        var user = await _context.Users
            .Include(u => u.Customer)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user == null)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("Your account is currently inactive. Please contact support.");
        }

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var tokenConfig = await _configurationService.GetTokenConfigAsync();
        var token = _jwtTokenService.GenerateUserToken(user, tokenConfig.TokenTimeoutMinutes);
        var expiresAt = tokenConfig.TokenTimeoutMinutes > 0
            ? DateTime.UtcNow.AddMinutes(tokenConfig.TokenTimeoutMinutes)
            : DateTime.UtcNow.AddDays(7);

        var enquiriesCount = await _context.Enquiries
            .CountAsync(e => e.UserId == user.Id || (user.CustomerId.HasValue && e.CustomerId == user.CustomerId.Value));

        return new UserLoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CompanyName = user.CompanyName,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                TotalEnquiries = enquiriesCount
            }
        };
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new NotFoundException("User account not found.");

        var enquiriesQuery = _context.Enquiries
            .Where(e => e.UserId == user.Id || (user.CustomerId.HasValue && e.CustomerId == user.CustomerId.Value));

        var totalEnquiries = await enquiriesQuery.CountAsync();
        var activeQuotations = await _context.Quotations
            .Where(q => user.CustomerId.HasValue && q.CustomerId == user.CustomerId.Value && (q.Status == QuotationStatus.Sent || q.Status == QuotationStatus.Viewed))
            .CountAsync();
        var activeProjects = await _context.Projects
            .Where(p => user.CustomerId.HasValue && p.CustomerId == user.CustomerId.Value && p.Status == ProjectStatus.InProgress)
            .CountAsync();

        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            CompanyName = user.CompanyName,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            TotalEnquiries = totalEnquiries,
            ActiveQuotations = activeQuotations,
            ActiveProjects = activeProjects
        };
    }

    public async Task<List<UserEnquirySummaryDto>> GetUserEnquiriesAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new NotFoundException("User account not found.");

        var enquiries = await _context.Enquiries
            .Include(e => e.Quotations)
            .Where(e => e.UserId == user.Id || (user.CustomerId.HasValue && e.CustomerId == user.CustomerId.Value))
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return enquiries.Select(e =>
        {
            var activeQuote = e.Quotations
                .OrderByDescending(q => q.CreatedAt)
                .FirstOrDefault();

            return new UserEnquirySummaryDto
            {
                Id = e.Id,
                ReferenceNumber = e.ReferenceNumber,
                ProjectTitle = e.ProjectTitle,
                ProjectDescription = e.ProjectDescription,
                RequiredTechnology = e.RequiredTechnology,
                EstimatedBudget = e.EstimatedBudget,
                Status = e.Status,
                StatusDisplay = e.Status switch
                {
                    EnquiryStatus.New => "Submitted",
                    EnquiryStatus.Reviewed => "Under Technical Review",
                    EnquiryStatus.Quoted => "Quotation Proposal Ready",
                    EnquiryStatus.FollowUp => "Discussion & Scope Refinement",
                    EnquiryStatus.Converted => "Project Active / In Progress",
                    EnquiryStatus.Closed => "Completed & Handed Over",
                    _ => e.Status.ToString()
                },
                CreatedAt = e.CreatedAt,
                QuotationNumber = activeQuote?.QuotationNumber,
                QuotationSecureToken = activeQuote?.SecureToken,
                QuotationAmount = activeQuote?.TotalAmount,
                QuotationStatus = activeQuote?.Status
            };
        }).ToList();
    }

    public async Task<bool> ChangePasswordAsync(int userId, UserChangePasswordRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new NotFoundException("User account not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BadRequestException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    // Admin Methods
    public async Task<PagedResult<AdminUserAccountDto>> GetAdminUsersAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        var query = _context.Users
            .Include(u => u.Enquiries)
            .Include(u => u.Customer)
                .ThenInclude(c => c!.Quotations)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(s)
                || u.Email.ToLower().Contains(s)
                || u.PhoneNumber.Contains(s)
                || (u.CompanyName != null && u.CompanyName.ToLower().Contains(s)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserAccountDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                CompanyName = u.CompanyName,
                IsActive = u.IsActive,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt,
                EnquiriesCount = u.Enquiries.Count + (u.Customer != null ? u.Customer.Enquiries.Count(e => e.UserId != u.Id) : 0),
                QuotationsCount = u.Customer != null ? u.Customer.Quotations.Count : 0
            })
            .ToListAsync();

        return new PagedResult<AdminUserAccountDto>(items, total, page, pageSize);
    }

    public async Task<AdminUserAccountDto> GetAdminUserByIdAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.Enquiries)
            .Include(u => u.Customer)
                .ThenInclude(c => c!.Quotations)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            throw new NotFoundException($"User account #{id} not found.");

        return new AdminUserAccountDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            CompanyName = user.CompanyName,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            EnquiriesCount = user.Enquiries.Count + (user.Customer != null ? user.Customer.Enquiries.Count(e => e.UserId != user.Id) : 0),
            QuotationsCount = user.Customer != null ? user.Customer.Quotations.Count : 0
        };
    }

    public async Task<List<UserEnquirySummaryDto>> GetAdminUserEnquiriesAsync(int userId)
    {
        return await GetUserEnquiriesAsync(userId);
    }

    public async Task<bool> ToggleUserStatusAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            throw new NotFoundException($"User account #{id} not found.");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return user.IsActive;
    }
}
