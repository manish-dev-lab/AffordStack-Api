using SoftwareServices.Application.Common;
using SoftwareServices.Application.DTOs.User;

namespace SoftwareServices.Application.Interfaces;

public interface IUserAuthService
{
    Task<bool> SendRegistrationOtpAsync(SendRegistrationOtpRequest request);
    Task<UserLoginResponse> RegisterAsync(UserRegisterRequest request);
    Task<UserLoginResponse> LoginAsync(UserLoginRequest request);
    Task<UserProfileDto> GetProfileAsync(int userId);
    Task<List<UserEnquirySummaryDto>> GetUserEnquiriesAsync(int userId);
    Task<bool> ChangePasswordAsync(int userId, UserChangePasswordRequest request);

    // Admin methods
    Task<PagedResult<AdminUserAccountDto>> GetAdminUsersAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<AdminUserAccountDto> GetAdminUserByIdAsync(int id);
    Task<List<UserEnquirySummaryDto>> GetAdminUserEnquiriesAsync(int userId);
    Task<bool> ToggleUserStatusAsync(int id);
}
