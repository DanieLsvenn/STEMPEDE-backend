using Stemkit.DTOs;

namespace Stemkit.Auth.Services.Interfaces
{
    public interface IExternalAuthService
    {
        Task<AuthResponse> GoogleLoginAsync(string idToken, string ipAddress);

    }
}
