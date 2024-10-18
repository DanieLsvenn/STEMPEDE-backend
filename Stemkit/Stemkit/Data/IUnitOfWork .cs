using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Stemkit.Repositories.Interfaces;

namespace Stemkit.Data
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

