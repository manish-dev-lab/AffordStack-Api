using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SoftwareServices.Application.Common;
using SoftwareServices.Application.Common.Exceptions;
using SoftwareServices.Application.DTOs.User;
using SoftwareServices.Application.Interfaces;
using SoftwareServices.Domain.Entities;
using SoftwareServices.Domain.Enums;
using System.Security.Cryptography;

namespace SoftwareServices.Application.Services;

public class UserAuthService : IUserAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfigurationService _configurationService;
    private readonly IMemoryCache _cache;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserAuthService> _logger;

    public UserAuthService(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IConfigurationService configurationService,
        IMemoryCache cache,
        IEmailService emailService,
        ILogger<UserAuthService> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _configurationService = configurationService;
        _cache = cache;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<bool> SendRegistrationOtpAsync(SendRegistrationOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new BadRequestException("Please enter a valid email address.");

        var email = request.Email.Trim().ToLower();

        // 1. Check if user already exists
        var existing = await _context.Users.AnyAsync(u => u.Email.ToLower() == email);
        if (existing)
        {
            throw new BadRequestException("An account with this email address already exists. Please sign in instead.");
        }

        // 2. Generate 6-digit numeric OTP
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var cacheKey = $"reg_otp:{email}";
        _cache.Set(cacheKey, code, TimeSpan.FromMinutes(10));

        // 3. Dispatch OTP verification email
        var subject = $"SoftwareServices - Verification Code: {code}";
        var body = $@"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; background-color: #f8fafc; padding: 20px; color: #1e293b;"">
  <div style=""max-width: 500px; margin: auto; background: #ffffff; border-radius: 16px; padding: 32px; border: 1px solid #e2e8f0; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);"">
    <div style=""text-align: center; margin-bottom: 24px;"">
      <h2 style=""color: #2563eb; margin: 0; font-size: 24px;"">SoftwareServices</h2>
      <p style=""font-size: 13px; color: #64748b; margin-top: 4px;"">Account Verification Code</p>
    </div>
    <p style=""font-size: 14px;"">Hello {request.FullName ?? "there"},</p>
    <p style=""font-size: 14px;"">Thank you for creating an account with SoftwareServices Platform. Please enter the following 6-digit verification code to confirm your email:</p>
    <div style=""text-align: center; margin: 28px 0;"">
      <span style=""display: inline-block; font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #1d4ed8; background: #eff6ff; padding: 12px 28px; border-radius: 12px; border: 1px dashed #bfdbfe;"">
        {code}
      </span>
    </div>
    <p style=""font-size: 12px; color: #64748b; text-align: center;"">This code is valid for <strong>10 minutes</strong>. If you did not request this account, please ignore this email.</p>
    <hr style=""border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;"" />
    <p style=""font-size: 11px; color: #94a3b8; text-align: center;"">&copy; {DateTime.UtcNow.Year} SoftwareServices Platform. All rights reserved.</p>
  </div>
</body>
</html>";

        try
        {
            await _emailService.SendEmailDirectAsync(email, request.FullName ?? "User", subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Email delivery failed for OTP: {Message}. OTP stored in cache for verification.", ex.Message);
        }

        return true;
    }

    public async Task<UserLoginResponse> RegisterAsync(UserRegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new BadRequestException("Email address is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new BadRequestException("Password is required.");

        if (request.Password.Length < 6)
            throw new BadRequestException("Password must be at least 6 characters long.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new BadRequestException("Full name is required.");

        var email = request.Email.Trim().ToLower();

        // 1. Verify OTP if provided
        if (!string.IsNullOrWhiteSpace(request.Otp))
        {
            var cacheKey = $"reg_otp:{email}";
            if (!_cache.TryGetValue(cacheKey, out string? cachedOtp) || cachedOtp != request.Otp.Trim())
            {
                throw new BadRequestException("Invalid or expired verification code. Please request a new code.");
            }
            _cache.Remove(cacheKey);
        }

        // 2. Check if user already exists
        var existingUser = await _context.Users.AnyAsync(u => u.Email.ToLower() == email);
        if (existingUser)
        {
            throw new BadRequestException("An account with this email address already exists. Please sign in instead.");
        }

        // 3. Customer Linking: Check if customer exists and ensure 1:1 relationship is not already taken
        Customer? customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == email);
        if (customer != null)
        {
            var customerClaimed = await _context.Users.AnyAsync(u => u.CustomerId == customer.Id);
            if (customerClaimed)
            {
                customer = null;
            }
        }

        if (customer == null)
        {
            customer = new Customer
            {
                FullName = request.FullName.Trim(),
                Email = email,
                PhoneNumber = !string.IsNullOrWhiteSpace(request.PhoneNumber) ? request.PhoneNumber.Trim() : "+91 0000000000",
                CompanyName = request.CompanyName?.Trim()
            };
            _context.Customers.Add(customer);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Customer profile creation note: {Message}", ex.Message);
            }
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = !string.IsNullOrWhiteSpace(request.PhoneNumber) ? request.PhoneNumber.Trim() : (customer?.PhoneNumber ?? string.Empty),
            CompanyName = request.CompanyName?.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CustomerId = customer?.Id,
            IsEmailVerified = true,
            IsActive = true,
            LastLoginAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "DbUpdateException during user registration");
            throw new BadRequestException("Unable to register account. This email or account may already exist.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during user registration");
            throw new BadRequestException($"Registration failed: {ex.Message}");
        }

        // Associate any existing past enquiries for this email with the newly registered user
        int pastCount = 0;
        try
        {
            var pastEnquiries = await _context.Enquiries
                .Include(e => e.Customer)
                .Where(e => e.UserId == null && (
                    (customer != null && e.CustomerId == customer.Id) ||
                    (e.Customer != null && e.Customer.Email.ToLower() == email)
                ))
                .ToListAsync();

            pastCount = pastEnquiries.Count;
            if (pastCount > 0)
            {
                foreach (var enq in pastEnquiries)
                {
                    enq.UserId = user.Id;
                    if (customer != null && enq.CustomerId != customer.Id)
                    {
                        enq.CustomerId = customer.Id;
                    }
                }
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception linkEx)
        {
            _logger.LogWarning("Notice while associating past enquiries during registration: {Message}", linkEx.Message);
        }

        int timeoutMinutes = 60;
        try
        {
            var tokenConfig = await _configurationService.GetTokenConfigAsync();
            if (tokenConfig.TokenTimeoutMinutes > 0)
                timeoutMinutes = tokenConfig.TokenTimeoutMinutes;
        }
        catch
        {
            // Fallback default
        }

        var token = _jwtTokenService.GenerateUserToken(user, timeoutMinutes);
        var expiresAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);

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
                TotalEnquiries = pastCount
            }
        };
    }

    public async Task<UserLoginResponse> LoginAsync(UserLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

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

        try
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Do not block login
        }

        int timeoutMinutes = 60;
        try
        {
            var tokenConfig = await _configurationService.GetTokenConfigAsync();
            if (tokenConfig.TokenTimeoutMinutes > 0)
                timeoutMinutes = tokenConfig.TokenTimeoutMinutes;
        }
        catch
        {
            // Fallback default
        }

        var token = _jwtTokenService.GenerateUserToken(user, timeoutMinutes);
        var expiresAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);

        var enquiriesCount = 0;
        try
        {
            enquiriesCount = await _context.Enquiries
                .CountAsync(e => e.UserId == user.Id || (user.CustomerId.HasValue && e.CustomerId == user.CustomerId.Value));
        }
        catch
        {
            // Ignore
        }

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

        var cleanEmail = user.Email.Trim().ToLower();

        var enquiriesQuery = _context.Enquiries
            .Include(e => e.Customer)
            .Where(e => e.UserId == user.Id ||
                        (user.CustomerId.HasValue && e.CustomerId == user.CustomerId.Value) ||
                        (e.Customer != null && e.Customer.Email.ToLower() == cleanEmail));

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

    public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateUserProfileRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new NotFoundException("User account not found.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new BadRequestException("Full name is required.");

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
        user.CompanyName = request.CompanyName?.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        // Update customer profile
        if (user.CustomerId.HasValue)
        {
            var customer = await _context.Customers.FindAsync(user.CustomerId.Value);
            if (customer != null)
            {
                customer.FullName = user.FullName;
                customer.PhoneNumber = user.PhoneNumber;
                customer.CompanyName = user.CompanyName;
                customer.UpdatedAt = DateTime.UtcNow;
            }
        }

        var oldEmail = user.Email.Trim().ToLower();
        var matchingCustomers = await _context.Customers.Where(c => c.Email.ToLower() == oldEmail).ToListAsync();
        foreach (var c in matchingCustomers)
        {
            c.FullName = user.FullName;
            c.PhoneNumber = user.PhoneNumber;
            c.CompanyName = user.CompanyName;
            c.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return await GetProfileAsync(userId);
    }

    public async Task<bool> RequestEmailChangeOtpAsync(int userId, RequestEmailChangeOtpRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new NotFoundException("User account not found.");

        if (string.IsNullOrWhiteSpace(request.NewEmail) || !request.NewEmail.Contains("@"))
            throw new BadRequestException("Please enter a valid email address.");

        var cleanNewEmail = request.NewEmail.Trim().ToLowerInvariant();

        if (user.Email.Trim().ToLowerInvariant() == cleanNewEmail)
            throw new BadRequestException("The new email address cannot be identical to your current email address.");

        var emailInUse = await _context.Users.AnyAsync(u => u.Email.ToLower() == cleanNewEmail && u.Id != userId);
        if (emailInUse)
            throw new BadRequestException("An account with this email address already exists. Please choose a different email.");

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var cacheKey = $"email_change_otp:{userId}:{cleanNewEmail}";
        _cache.Set(cacheKey, code, TimeSpan.FromMinutes(10));

        var subject = $"SoftwareServices - Email Verification Code: {code}";
        var body = $@"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; background-color: #f8fafc; padding: 20px; color: #1e293b;"">
  <div style=""max-width: 500px; margin: auto; background: #ffffff; border-radius: 16px; padding: 32px; border: 1px solid #e2e8f0; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);"">
    <div style=""text-align: center; margin-bottom: 24px;"">
      <h2 style=""color: #2563eb; margin: 0; font-size: 24px;"">SoftwareServices</h2>
      <p style=""font-size: 13px; color: #64748b; margin-top: 4px;"">Email Address Change Request</p>
    </div>
    <p style=""font-size: 14px;"">Hello {user.FullName},</p>
    <p style=""font-size: 14px;"">We received a request to update your account email address to <strong>{cleanNewEmail}</strong>. Please enter the following 6-digit verification code to confirm this change:</p>
    <div style=""text-align: center; margin: 28px 0;"">
      <span style=""display: inline-block; font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #2563eb; background: #eff6ff; padding: 12px 28px; border-radius: 12px; border: 1px dashed #bfdbfe;"">
        {code}
      </span>
    </div>
    <p style=""font-size: 12px; color: #64748b; text-align: center;"">This code will expire in <strong>10 minutes</strong>. Once verified, all your project updates, invoices, and enquiries will be linked to this new email.</p>
    <hr style=""border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;"" />
    <p style=""font-size: 11px; color: #94a3b8; text-align: center;"">&copy; {DateTime.UtcNow.Year} SoftwareServices Platform. All rights reserved.</p>
  </div>
</body>
</html>";

        try
        {
            await _emailService.SendEmailDirectAsync(cleanNewEmail, user.FullName, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Email delivery failed for email change OTP: {Message}", ex.Message);
        }

        return true;
    }

    public async Task<UserLoginResponse> ConfirmEmailChangeAsync(int userId, ConfirmEmailChangeRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new NotFoundException("User account not found.");

        if (string.IsNullOrWhiteSpace(request.NewEmail) || string.IsNullOrWhiteSpace(request.Otp))
            throw new BadRequestException("New email and verification code are required.");

        var cleanNewEmail = request.NewEmail.Trim().ToLowerInvariant();
        var cacheKey = $"email_change_otp:{userId}:{cleanNewEmail}";

        if (!_cache.TryGetValue(cacheKey, out string? cachedOtp) || cachedOtp != request.Otp.Trim())
            throw new BadRequestException("Invalid or expired verification code. Please request a new code.");

        var emailInUse = await _context.Users.AnyAsync(u => u.Email.ToLower() == cleanNewEmail && u.Id != userId);
        if (emailInUse)
            throw new BadRequestException("An account with this email address already exists. Please choose a different email.");

        var oldEmail = user.Email.Trim().ToLowerInvariant();
        user.Email = cleanNewEmail;
        user.IsEmailVerified = true;
        user.UpdatedAt = DateTime.UtcNow;

        if (user.CustomerId.HasValue)
        {
            var customer = await _context.Customers.FindAsync(user.CustomerId.Value);
            if (customer != null)
            {
                customer.Email = cleanNewEmail;
                customer.UpdatedAt = DateTime.UtcNow;
            }
        }

        var matchingCustomers = await _context.Customers.Where(c => c.Email.ToLower() == oldEmail).ToListAsync();
        foreach (var c in matchingCustomers)
        {
            c.Email = cleanNewEmail;
            c.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _cache.Remove(cacheKey);

        int timeoutMinutes = 60;
        try
        {
            var tokenConfig = await _configurationService.GetTokenConfigAsync();
            if (tokenConfig.TokenTimeoutMinutes > 0)
                timeoutMinutes = tokenConfig.TokenTimeoutMinutes;
        }
        catch { }

        var token = _jwtTokenService.GenerateUserToken(user, timeoutMinutes);
        var expiresAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);
        var updatedProfile = await GetProfileAsync(userId);

        return new UserLoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = updatedProfile
        };
    }

    public async Task<List<UserEnquirySummaryDto>> GetUserEnquiriesAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new NotFoundException("User account not found.");

        var cleanEmail = user.Email.Trim().ToLower();

        var enquiries = await _context.Enquiries
            .Include(e => e.Quotations)
                .ThenInclude(q => q.Project)
                    .ThenInclude(p => p!.Milestones)
            .Include(e => e.Customer)
            .Where(e => e.UserId == user.Id ||
                        (user.CustomerId.HasValue && e.CustomerId == user.CustomerId.Value) ||
                        (e.Customer != null && e.Customer.Email.ToLower() == cleanEmail))
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        // Auto-link any guest enquiries matching by email
        bool needSave = false;
        foreach (var enq in enquiries)
        {
            if (enq.UserId == null)
            {
                enq.UserId = user.Id;
                needSave = true;
            }
        }
        if (needSave)
        {
            try { await _context.SaveChangesAsync(); } catch { }
        }

        return enquiries.Select(e =>
        {
            var activeQuote = e.Quotations
                .OrderByDescending(q => q.CreatedAt)
                .FirstOrDefault();
            var project = activeQuote?.Project;

            string effectiveStatus;
            string statusDisplay;

            if (project != null)
            {
                if (project.Status == ProjectStatus.Completed || e.Status == EnquiryStatus.Closed)
                {
                    effectiveStatus = "Completed";
                    statusDisplay = "Delivered / Completed";
                }
                else if (project.Status == ProjectStatus.InProgress)
                {
                    effectiveStatus = "InProgress";
                    statusDisplay = "In Progress";
                }
                else if (project.Status == ProjectStatus.OnHold)
                {
                    effectiveStatus = "OnHold";
                    statusDisplay = "On Hold";
                }
                else if (project.Status == ProjectStatus.Cancelled)
                {
                    effectiveStatus = "Cancelled";
                    statusDisplay = "Cancelled";
                }
                else
                {
                    effectiveStatus = "NotStarted";
                    statusDisplay = "Project Provisioned";
                }
            }
            else if (activeQuote != null)
            {
                if (activeQuote.Status == QuotationStatus.Accepted)
                {
                    effectiveStatus = "Accepted";
                    statusDisplay = "Quotation Accepted";
                }
                else if (activeQuote.Status == QuotationStatus.Rejected)
                {
                    effectiveStatus = "Rejected";
                    statusDisplay = "Quotation Declined";
                }
                else if (activeQuote.Status == QuotationStatus.Sent || activeQuote.Status == QuotationStatus.Viewed)
                {
                    effectiveStatus = "Quoted";
                    statusDisplay = "Quotation Proposal Ready";
                }
                else
                {
                    effectiveStatus = e.Status.ToString();
                    statusDisplay = e.Status switch
                    {
                        EnquiryStatus.New => "Submitted",
                        EnquiryStatus.Reviewed => "Under Technical Review",
                        EnquiryStatus.Quoted => "Quotation Proposal Ready",
                        EnquiryStatus.FollowUp => "Discussion & Scope Refinement",
                        EnquiryStatus.Converted => "Project Active / In Progress",
                        EnquiryStatus.Closed => "Completed & Handed Over",
                        _ => e.Status.ToString()
                    };
                }
            }
            else
            {
                effectiveStatus = e.Status.ToString();
                statusDisplay = e.Status switch
                {
                    EnquiryStatus.New => "Submitted",
                    EnquiryStatus.Reviewed => "Under Technical Review",
                    EnquiryStatus.Quoted => "Quotation Proposal Ready",
                    EnquiryStatus.FollowUp => "Discussion & Scope Refinement",
                    EnquiryStatus.Converted => "Project Active / In Progress",
                    EnquiryStatus.Closed => "Completed & Handed Over",
                    _ => e.Status.ToString()
                };
            }

            var finalEnquiryStatus = (project != null && project.Status == ProjectStatus.Completed) 
                ? EnquiryStatus.Closed 
                : ((project != null && project.Status == ProjectStatus.InProgress && e.Status != EnquiryStatus.Closed) ? EnquiryStatus.Converted : e.Status);

            return new UserEnquirySummaryDto
            {
                Id = e.Id,
                ReferenceNumber = e.ReferenceNumber,
                ProjectTitle = e.ProjectTitle,
                ProjectDescription = e.ProjectDescription,
                RequiredTechnology = e.RequiredTechnology,
                EstimatedBudget = e.EstimatedBudget,
                Status = finalEnquiryStatus,
                EffectiveStatus = effectiveStatus,
                StatusDisplay = statusDisplay,
                CreatedAt = e.CreatedAt,
                QuotationNumber = activeQuote?.QuotationNumber,
                QuotationSecureToken = activeQuote?.SecureToken,
                QuotationAmount = activeQuote?.TotalAmount,
                QuotationStatus = activeQuote?.Status,

                ProjectId = project?.Id,
                ProjectNumber = project?.ProjectNumber,
                ProjectStatus = project?.Status,
                ProgressPercentage = project?.ProgressPercentage,
                ExpectedCompletionDate = project?.ExpectedCompletionDate,
                Milestones = project?.Milestones.OrderBy(m => m.DueDate).Select(m => new SoftwareServices.Application.DTOs.Project.ProjectMilestoneDto
                {
                    Id = m.Id,
                    Title = m.Title,
                    Description = m.Description,
                    DueDate = m.DueDate,
                    IsCompleted = m.IsCompleted,
                    CompletedAt = m.CompletedAt,
                    Amount = m.Amount
                }).ToList() ?? new()
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

    public async Task<bool> ForgotPasswordAsync(UserForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new BadRequestException("Please enter your registered email address.");

        var cleanEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == cleanEmail);
        if (user == null)
            throw new NotFoundException("No account registered with this email address was found. Please check your spelling or sign up.");

        if (!user.IsActive)
            throw new BadRequestException("This account is currently deactivated. Please contact administrator support.");

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var cacheKey = $"pwd_reset_otp:{cleanEmail}";
        _cache.Set(cacheKey, code, TimeSpan.FromMinutes(10));

        var subject = $"SoftwareServices - Password Reset Code: {code}";
        var body = $@"
<!DOCTYPE html>
<html>
<body style=""font-family: Arial, sans-serif; background-color: #f8fafc; padding: 20px; color: #1e293b;"">
  <div style=""max-width: 500px; margin: auto; background: #ffffff; border-radius: 16px; padding: 32px; border: 1px solid #e2e8f0; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);"">
    <div style=""text-align: center; margin-bottom: 24px;"">
      <h2 style=""color: #2563eb; margin: 0; font-size: 24px;"">SoftwareServices</h2>
      <p style=""font-size: 13px; color: #64748b; margin-top: 4px;"">Password Reset Request</p>
    </div>
    <p style=""font-size: 14px;"">Hello {user.FullName},</p>
    <p style=""font-size: 14px;"">We received a request to reset the password for your SoftwareServices client account. Please enter the following 6-digit verification code to create your new password:</p>
    <div style=""text-align: center; margin: 28px 0;"">
      <span style=""display: inline-block; font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #dc2626; background: #fef2f2; padding: 12px 28px; border-radius: 12px; border: 1px dashed #fca5a5;"">
        {code}
      </span>
    </div>
    <p style=""font-size: 12px; color: #64748b; text-align: center;"">This code will expire in <strong>10 minutes</strong>. If you did not request a password reset, you can safely ignore this email — your account remains secure.</p>
    <hr style=""border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;"" />
    <p style=""font-size: 11px; color: #94a3b8; text-align: center;"">&copy; {DateTime.UtcNow.Year} SoftwareServices Platform. All rights reserved.</p>
  </div>
</body>
</html>";

        try
        {
            await _emailService.SendEmailDirectAsync(cleanEmail, user.FullName, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Email delivery failed for password reset OTP: {Message}", ex.Message);
        }

        return true;
    }

    public async Task<bool> ResetPasswordAsync(UserResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new BadRequestException("Email address is required.");

        if (string.IsNullOrWhiteSpace(request.Otp))
            throw new BadRequestException("Verification code is required.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            throw new BadRequestException("New password must be at least 6 characters long.");

        var cleanEmail = request.Email.Trim().ToLowerInvariant();
        var cacheKey = $"pwd_reset_otp:{cleanEmail}";

        if (!_cache.TryGetValue(cacheKey, out string? cachedOtp) || cachedOtp != request.Otp.Trim())
            throw new BadRequestException("Invalid or expired verification code. Please request a new code.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == cleanEmail);
        if (user == null)
            throw new NotFoundException("User account not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _cache.Remove(cacheKey);

        return true;
    }

    // Admin Methods
    public async Task<PagedResult<AdminUserAccountDto>> GetAdminUsersAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        try
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
            if (total == 0)
            {
                return new PagedResult<AdminUserAccountDto>(new List<AdminUserAccountDto>(), 0, page, pageSize);
            }

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = users.Select(u => new AdminUserAccountDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                CompanyName = u.CompanyName,
                IsActive = u.IsActive,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt,
                EnquiriesCount = u.Enquiries?.Count ?? 0,
                QuotationsCount = u.Customer?.Quotations?.Count ?? 0
            }).ToList();

            return new PagedResult<AdminUserAccountDto>(items, total, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying admin users");
            return new PagedResult<AdminUserAccountDto>(new List<AdminUserAccountDto>(), 0, page, pageSize);
        }
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
