using Application.DTOs.Auth;
using Domain.Entities;

namespace Application.Authentication.Services.Interfaces
{
    public interface IRefreshTokenService
    {
        RefreshToken GenerateRefreshToken(int userId, string ipAddress);
        Task SaveRefreshTokenAsync(RefreshToken refreshToken);
        Task<bool> ValidateRefreshTokenAsync(string token, int userId);
        Task InvalidateRefreshTokenAsync(string token, string ipAddress);
        Task<AuthResponseDto> RefreshTokensAsync(string token, string ipAddress);
    }
}
