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
}
