using Application.DTOs.Auth;

namespace Application.Authentication.Services.Interfaces
{
    public interface IExternalAuthService
    {
        Task<AuthResponseDto> GoogleLoginAsync(string idToken, string ipAddress);

    }
}
