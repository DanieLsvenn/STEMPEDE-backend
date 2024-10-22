using Stemkit.DTOs;

namespace Stemkit.Auth.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(UserRegistrationDto registrationDto, string ipAddress);
        Task<AuthResponse> LoginAsync(UserLoginDto loginDto, string ipAddress);
        Task<AuthResponse> LogoutAsync(string refreshToken, string ipAddress);
        Task<AuthResponse> RefreshTokenAsync(string token, string ipAddress);

    }
}
