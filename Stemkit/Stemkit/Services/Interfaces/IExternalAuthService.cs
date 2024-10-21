using Stemkit.DTOs;

namespace Stemkit.Services.Interfaces
{
    public interface IExternalAuthService
    {
        Task<AuthResponse> GoogleLoginAsync(string idToken, string ipAddress);

    }
}
