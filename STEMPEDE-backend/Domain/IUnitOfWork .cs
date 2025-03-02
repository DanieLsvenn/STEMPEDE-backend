using Domain.IRepositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Domain
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<T> GetRepository<T>() where T : class;
        IRefreshTokenRepository RefreshTokens { get; }
        int Complete();
        Task<int> CompleteAsync();
        IDbContextTransaction BeginTransaction();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}

