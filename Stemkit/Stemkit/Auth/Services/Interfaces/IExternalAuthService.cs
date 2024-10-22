using Stemkit.DTOs.AuthDTO;

namespace Stemkit.Auth.Services.Interfaces
{
    public interface IExternalAuthService
    {
        Task<AuthResponse> GoogleLoginAsync(string idToken, string ipAddress);

    }
}
