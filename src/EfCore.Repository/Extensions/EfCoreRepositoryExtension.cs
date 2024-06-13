using Base.Repository.Abstractions;
using Base.Repository.Options;
using EfCore.Repository.Abstractions;
using EfCore.Repository.Concretes;
using EfCore.Repository.Factory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Repository.Extensions
{
    public static class EfCoreRepositoryExtension
    {
        public static IServiceCollection EfCoreRepositoryServiceRegistration<TEntity, TDbContext>(this IServiceCollection services, ServiceLifetime serviceLifetime, DatabaseOptions? databaseOptions = null)
            where TEntity : class, new()
            where TDbContext : DbContext
        {


            if (databaseOptions is not null)
            {
                services.Add(new ServiceDescriptor(
                   typeof(IBaseReadRepository<TEntity>),
                   sp =>
                   {
                       return new BaseReadRepository<TEntity>(databaseOptions, sp);
                   },
                   serviceLifetime
               ));

                services.Add(new ServiceDescriptor(
                   typeof(IBaseWriteRepository<TEntity>),
                   sp =>
                   {
                       return new BaseWriteRepository<TEntity>(databaseOptions, sp);
                   },
                   serviceLifetime
                ));

                services.Add(new ServiceDescriptor(
                   typeof(IWriteRepository<TEntity>),
                   sp =>
                   {
                       return new WriteRepository<TEntity>(databaseOptions, sp);
                   },
                   serviceLifetime
                ));

                services.Add(new ServiceDescriptor(
                  typeof(IReadRepository<TEntity>),
                  sp =>
                  {
                      return new ReadRepository<TEntity>(databaseOptions, sp);
                  },
                  serviceLifetime
                ));


                services.Add(new ServiceDescriptor(
                  typeof(IDbReadRepository<TEntity>),
                  sp =>
                  {
                      return new DbReadRepository<TEntity>(databaseOptions, sp);
                  },
                  serviceLifetime
                ));


                services.Add(new ServiceDescriptor(
                  typeof(IDbWriteRepository<TEntity>),
                  sp =>
                  {
                      return new DbWriteRepository<TEntity>(databaseOptions, sp);
                  },
                  serviceLifetime
                ));

                services.Add(new ServiceDescriptor(
                  typeof(IUnitOfWork<TEntity>),
                  sp =>
                  {
                      TDbContext dbContext = (TDbContext)Activator.CreateInstance(typeof(TDbContext));
                      return RepositoryFactory<TDbContext>.CreateUnitOfWork<TEntity>(databaseOptions, sp);
                  },
                   serviceLifetime
               ));

                return services;
            }
            else
            {
                TDbContext dbContext = (TDbContext)Activator.CreateInstance(typeof(TDbContext));

                services.Add(new ServiceDescriptor(
                   typeof(IBaseReadRepository<TEntity>),
                   sp =>
                   {
                       return new BaseReadRepository<TEntity>(dbContext, sp);
                   },
                   serviceLifetime
               ));

                services.Add(new ServiceDescriptor(
                   typeof(IBaseWriteRepository<TEntity>),
                   sp =>
                   {
                       return new BaseWriteRepository<TEntity>(dbContext, sp);
                   },
                   serviceLifetime
                ));

                services.Add(new ServiceDescriptor(
                   typeof(IWriteRepository<TEntity>),
                   sp =>
                   {
                       return new WriteRepository<TEntity>(dbContext, sp);
                   },
                   serviceLifetime
                ));

                services.Add(new ServiceDescriptor(
                  typeof(IReadRepository<TEntity>),
                  sp =>
                  {
                      return new ReadRepository<TEntity>(dbContext, sp);
                  },
                  serviceLifetime
                ));


                services.Add(new ServiceDescriptor(
                  typeof(IDbReadRepository<TEntity>),
                  sp =>
                  {
                      return new DbReadRepository<TEntity>(dbContext, sp);
                  },
                  serviceLifetime
                ));


                services.Add(new ServiceDescriptor(
                  typeof(IDbWriteRepository<TEntity>),
                  sp =>
                  {
                      return new DbWriteRepository<TEntity>(dbContext, sp);
                  },
                  serviceLifetime
                ));

                services.Add(new ServiceDescriptor(
                   typeof(IUnitOfWork<TEntity>),
                   sp =>
                   {
                       return RepositoryFactory<TDbContext>.CreateUnitOfWork<TEntity>(dbContext, sp);
                   },
                    serviceLifetime
                ));

                return services;
            }

        }
    }
}
