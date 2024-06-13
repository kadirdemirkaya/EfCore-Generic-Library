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
        //IReadRepository<TEntity> GetReadRepository(bool isServiceProvider);
        //IReadRepository<TEntity> GetReadRepository(bool isDatabaseOptions, bool isServiceProvider);

        IWriteRepository<TEntity> GetWriteRepository();
        //IWriteRepository<TEntity> GetWriteRepository(bool isServiceProvider);
        //IWriteRepository<TEntity> GetWriteRepository(bool isDatabaseOptions, bool isServiceProvider);

        IBaseReadRepository<TEntity> GetBaseReadRepository();
        //IBaseReadRepository<TEntity> GetBaseReadRepository(bool isServiceProvider);
        //IBaseReadRepository<TEntity> GetBaseReadRepository(bool isDatabaseOptions, bool isServiceProvider);

        IBaseWriteRepository<TEntity> GetBaseWriteRepository();
        //IBaseWriteRepository<TEntity> GetBaseWriteRepository(bool isServiceProvider);
        //IBaseWriteRepository<TEntity> GetBaseWriteRepository(bool isDatabaseOptions, bool isServiceProvider);

        IDbReadRepository<TEntity> GetDbReadRepository();
        //IDbReadRepository<TEntity> GetDbReadRepository(bool isServiceProvider);
        //IDbReadRepository<TEntity> GetDbReadRepository(bool isDatabaseOptions, bool isServiceProvider);

        IDbWriteRepository<TEntity> GetDbWriteRepository();
        //IDbWriteRepository<TEntity> GetDbWriteRepository(bool isServiceProvider);
        //IDbWriteRepository<TEntity> GetDbWriteRepository(bool isDatabaseOptions, bool isServiceProvider);
    }
}
