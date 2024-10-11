using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using SWP391__StempedeKit.Repositories.Interfaces;

namespace SWP391__StempedeKit.Data
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<T> GetRepository<T>() where T : class;
        int Complete();
        Task<int> CompleteAsync();
        IDbContextTransaction BeginTransaction();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}

