using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Base.Repository.Abstractions
{
    public interface IBaseWriteRepository<TEntity> : ITable
        where TEntity : class
    {
        Task<bool> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default);

        Task<bool> ExecuteCommandAsync(string command, params object[] parameters);

        Task<bool> ExecuteCommandAsync(string command, IEnumerable<object> parameters, CancellationToken cancellationToken = default);


        Task<IDbContextTransaction> BeginTransactionAsync(
             IsolationLevel isolationLevel = IsolationLevel.Unspecified,
             CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

        void Remove(TEntity entity);

        void ClearChangeTracker();

        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task<bool> SaveChangesAsync();
    }
}
