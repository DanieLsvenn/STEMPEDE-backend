using SWP391__StempedeKit.DTOs;

namespace SWP391__StempedeKit.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(UserRegistrationDto registrationDto);
        Task<AuthResponse> LoginAsync(UserLoginDto loginDto);
    }

}
