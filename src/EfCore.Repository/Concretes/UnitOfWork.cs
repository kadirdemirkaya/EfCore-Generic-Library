using Base.Repository.Abstractions;
using Base.Repository.Options;
using EfCore.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Repository.Concretes
{
    public sealed class UnitOfWork<TEntity> : IUnitOfWork<TEntity>
        where TEntity : class, new()
    {
        public DbContext _dbContext { get; private set; }
        public IServiceProvider _serviceProvider { get; private set; }
        public DatabaseOptions _databaseOptions { get; private set; }

        public UnitOfWork(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public UnitOfWork(DbContext dbContext, IServiceProvider serviceProvider)
        {
            _dbContext = dbContext;
            _serviceProvider = serviceProvider;
        }

        public UnitOfWork(DatabaseOptions databaseOptions, IServiceProvider serviceProvider)
        {
            _databaseOptions = databaseOptions;
            _serviceProvider = serviceProvider;
        }

        public ITable GetTable() => new Table(_dbContext);

        public IBaseReadRepository<TEntity> GetBaseReadRepository() => _serviceProvider.GetRequiredService<IBaseReadRepository<TEntity>>();

        public IBaseWriteRepository<TEntity> GetBaseWriteRepository() => _serviceProvider.GetRequiredService<IBaseWriteRepository<TEntity>>();

        public IDbReadRepository<TEntity> GetDbReadRepository() => _serviceProvider.GetRequiredService<IDbReadRepository<TEntity>>();

        public IDbWriteRepository<TEntity> GetDbWriteRepository() => _serviceProvider.GetRequiredService<IDbWriteRepository<TEntity>>();

        public IReadRepository<TEntity> GetReadRepository() => _serviceProvider.GetRequiredService<IReadRepository<TEntity>>();

        public IWriteRepository<TEntity> GetWriteRepository() => _serviceProvider.GetRequiredService<IWriteRepository<TEntity>>();

        public async Task<bool> SaveChangesAsync() => await _dbContext.SaveChangesAsync() > 0;
    }
}
