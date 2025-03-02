using Domain.Entities;

namespace Domain.IRepositories
{
    public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
    {
        Task<RefreshToken> GetByTokenAsync(string token);
        Task RemoveAllByUserIdAsync(int userId);
    }
}
