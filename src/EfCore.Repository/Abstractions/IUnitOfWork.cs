using Base.Repository.Abstractions;
using Base.Repository.Options;

namespace EfCore.Repository.Abstractions
{
    public interface IUnitOfWork<TEntity>
        where TEntity : class, new()
    {
        ITable GetTable();

        Task<bool> SaveChangesAsync();

        IReadRepository<TEntity> GetReadRepository();

        IWriteRepository<TEntity> GetWriteRepository();

        IBaseReadRepository<TEntity> GetBaseReadRepository();

        IBaseWriteRepository<TEntity> GetBaseWriteRepository();

        IDbReadRepository<TEntity> GetDbReadRepository();

        IDbWriteRepository<TEntity> GetDbWriteRepository();
    }
}
