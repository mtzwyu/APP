using OlapAnalytics.Application.DTOs.Auth;

namespace OlapAnalytics.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task UpdateSettingsAsync(int userId, SettingsRequest request);
    Task<UserSettingsDto> GetSettingsAsync(int userId);
}
