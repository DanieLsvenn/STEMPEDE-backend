using Stemkit.DTOs.Auth;

namespace Stemkit.Auth.Services.Interfaces
{
    public interface IExternalAuthService
    {
        Task<AuthResponse> GoogleLoginAsync(string idToken, string ipAddress);

    }
}
