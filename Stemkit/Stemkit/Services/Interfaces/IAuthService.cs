using Stemkit.DTOs;

namespace Stemkit.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(UserRegistrationDto registrationDto);
        Task<AuthResponse> LoginAsync(UserLoginDto loginDto);
    }
}
