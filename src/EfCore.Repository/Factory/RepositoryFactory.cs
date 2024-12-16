using Base.Repository.Options;
using EfCore.Repository.Concretes;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Repository.Factory
{
    public static class RepositoryFactory<TDbContext>
        where TDbContext : DbContext
    {
        public static UnitOfWork<TEntity> CreateUnitOfWork<TEntity>(TDbContext dbContext, IServiceProvider? serviceProvider = null)
                   where TEntity : class, new()
        {
            if (serviceProvider is not null)
                return new UnitOfWork<TEntity>(dbContext, serviceProvider);
            return new UnitOfWork<TEntity>(dbContext);
        }

        public static UnitOfWork<TEntity> CreateUnitOfWork<TEntity>(DatabaseOptions databaseOptions, IServiceProvider? serviceProvider = null)
                where TEntity : class, new()
        {
            return new UnitOfWork<TEntity>(databaseOptions, serviceProvider);
        }
    }

    public class RepositoryNoneStaticFactory<TDbContext>
       where TDbContext : DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        public RepositoryNoneStaticFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public UnitOfWork<TEntity> CreateUnitOfWork<TEntity>(TDbContext dbContext)
        where TEntity : class, new()
        {
            return new UnitOfWork<TEntity>(dbContext, _serviceProvider);
        }

        public UnitOfWork<TEntity> CreateUnitOfWork<TEntity>(DatabaseOptions databaseOptions)
            where TEntity : class, new()
        {
            return new UnitOfWork<TEntity>(databaseOptions, _serviceProvider);
        }
    }
}
