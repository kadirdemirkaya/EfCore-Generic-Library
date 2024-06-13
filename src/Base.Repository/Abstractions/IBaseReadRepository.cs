using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Base.Repository.Abstractions
{
    public interface IBaseReadRepository<TEntity> : ITable
        where TEntity : class
    {
        Task<List<TEntity>> ExecuteQueryAsync(string query);
        Task<List<TEntity>> ExecuteQueryAsync(string query, params object[] parameters);
        IQueryable<TEntity> GetQueryable();
        Task<List<TEntity>> GetListAsync(CancellationToken cancellationToken = default);
        Task<TEntity> GetByIdAsync(object id, CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(CancellationToken cancellationToken = default);
        Task<int> CountAsync(CancellationToken cancellationToken = default);

    }
}
