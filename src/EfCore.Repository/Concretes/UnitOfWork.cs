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
            _dbContext = databaseOptions?.Connection as DbContext;
        }

        public ITable GetTable() => new Table(_dbContext);

        public IBaseReadRepository<TEntity> GetBaseReadRepository() => Resolve<IBaseReadRepository<TEntity>>(() => new BaseReadRepository<TEntity>(_dbContext));

        public IBaseWriteRepository<TEntity> GetBaseWriteRepository() => Resolve<IBaseWriteRepository<TEntity>>(() => new BaseWriteRepository<TEntity>(_dbContext));

        public IDbReadRepository<TEntity> GetDbReadRepository() => Resolve<IDbReadRepository<TEntity>>(() => new DbReadRepository<TEntity>(_dbContext));

        public IDbWriteRepository<TEntity> GetDbWriteRepository() => Resolve<IDbWriteRepository<TEntity>>(() => new DbWriteRepository<TEntity>(_dbContext));

        public IReadRepository<TEntity> GetReadRepository() => Resolve<IReadRepository<TEntity>>(() => new ReadRepository<TEntity>(_dbContext));

        public IWriteRepository<TEntity> GetWriteRepository() => Resolve<IWriteRepository<TEntity>>(() => new WriteRepository<TEntity>(_dbContext));

        public async Task<bool> SaveChangesAsync() => await _dbContext.SaveChangesAsync().ConfigureAwait(false) > 0;

        private TRepository Resolve<TRepository>(Func<TRepository> createOnContext)
            where TRepository : notnull
            => _serviceProvider is null ? createOnContext() : _serviceProvider.GetRequiredService<TRepository>();
    }
}
