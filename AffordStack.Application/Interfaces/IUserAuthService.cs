using AffordStack.Application.Common;
using AffordStack.Application.DTOs.User;

namespace AffordStack.Application.Interfaces;

public interface IUserAuthService
{
    Task<bool> SendRegistrationOtpAsync(SendRegistrationOtpRequest request);
    Task<UserLoginResponse> RegisterAsync(UserRegisterRequest request);
    Task<UserLoginResponse> LoginAsync(UserLoginRequest request);
    Task<UserProfileDto> GetProfileAsync(int userId);
    Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateUserProfileRequest request);
    Task<bool> RequestEmailChangeOtpAsync(int userId, RequestEmailChangeOtpRequest request);
    Task<UserLoginResponse> ConfirmEmailChangeAsync(int userId, ConfirmEmailChangeRequest request);
    Task<List<UserEnquirySummaryDto>> GetUserEnquiriesAsync(int userId);
    Task<bool> ChangePasswordAsync(int userId, UserChangePasswordRequest request);
    Task<bool> ForgotPasswordAsync(UserForgotPasswordRequest request);
    Task<bool> ResetPasswordAsync(UserResetPasswordRequest request);

    // Admin methods
    Task<PagedResult<AdminUserAccountDto>> GetAdminUsersAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<AdminUserAccountDto> GetAdminUserByIdAsync(int id);
    Task<List<UserEnquirySummaryDto>> GetAdminUserEnquiriesAsync(int userId);
    Task<bool> ToggleUserStatusAsync(int id);
}
