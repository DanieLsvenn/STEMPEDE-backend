using Stemkit.Models;

namespace Stemkit.Repositories.Interfaces
{
    public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
    {
        Task<RefreshToken> GetByTokenAsync(string token);
        Task RemoveAllByUserIdAsync(int userId);
    }
}
