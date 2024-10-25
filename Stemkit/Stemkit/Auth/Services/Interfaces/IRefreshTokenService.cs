using Stemkit.DTOs.Auth;
using Stemkit.Models;

namespace Stemkit.Auth.Services.Interfaces
{
    public interface IRefreshTokenService
    {
        RefreshToken GenerateRefreshToken(int userId, string ipAddress);
        Task SaveRefreshTokenAsync(RefreshToken refreshToken);
        Task<bool> ValidateRefreshTokenAsync(string token, int userId);
        Task InvalidateRefreshTokenAsync(string token, string ipAddress);
        Task<AuthResponse> RefreshTokensAsync(string token, string ipAddress);
    }
}
