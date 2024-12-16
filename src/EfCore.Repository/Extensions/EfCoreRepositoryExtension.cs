using Base.Repository.Abstractions;
using Base.Repository.Options;
using EfCore.Repository.Abstractions;
using EfCore.Repository.Concretes;
using EfCore.Repository.Factory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Reflection.Metadata;

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

        public static IServiceCollection EfCoreRepositoryServiceRegistration<TBaseType, TDbContext>(this IServiceCollection services, ServiceLifetime serviceLifetime, params Assembly[] assemblies)
             where TDbContext : DbContext
        {
            foreach (var assembly in assemblies)
            {
                var entityTypes = assembly.GetTypes()
                                          .Where(t => typeof(TBaseType).IsAssignableFrom(t) && !t.IsInterface)
                                          .ToList();

                foreach (var entityType in entityTypes)
                {
                    TDbContext dbContext = (TDbContext)Activator.CreateInstance(typeof(TDbContext))!;

                    var baseReadIRepositoryType = typeof(IBaseReadRepository<>).MakeGenericType(entityType);
                    var baseWriteIRepositoryType = typeof(IBaseWriteRepository<>).MakeGenericType(entityType);
                    var writeIRepositoryType = typeof(IWriteRepository<>).MakeGenericType(entityType);
                    var readIRepositoryType = typeof(IReadRepository<>).MakeGenericType(entityType);
                    var dbReadIRepositoryType = typeof(IDbReadRepository<>).MakeGenericType(entityType);
                    var dbWriteIRepositoryType = typeof(IDbWriteRepository<>).MakeGenericType(entityType);
                    var iUnitOfWork = typeof(IUnitOfWork<>).MakeGenericType(entityType);

                    var baseReadRepositoryType = typeof(BaseReadRepository<>).MakeGenericType(entityType);
                    var baseWriteRepositoryType = typeof(BaseWriteRepository<>).MakeGenericType(entityType);
                    var writeRepositoryType = typeof(WriteRepository<>).MakeGenericType(entityType);
                    var readRepositoryType = typeof(ReadRepository<>).MakeGenericType(entityType);
                    var dbReadRepositoryType = typeof(DbReadRepository<>).MakeGenericType(entityType);
                    var dbWriteRepositoryType = typeof(DbWriteRepository<>).MakeGenericType(entityType);
                    var unitOfWork = typeof(RepositoryFactory<>).MakeGenericType(dbContext.GetType());

                    services.AddScoped(typeof(RepositoryNoneStaticFactory<>));

                    services.Add(new ServiceDescriptor(
                        baseReadIRepositoryType,
                        sp => Activator.CreateInstance(baseReadRepositoryType, dbContext, sp)!,
                        serviceLifetime
                    ));
                    services.Add(new ServiceDescriptor(
                        baseWriteIRepositoryType,
                        sp => Activator.CreateInstance(baseWriteRepositoryType, dbContext, sp)!,
                        serviceLifetime
                    ));
                    services.Add(new ServiceDescriptor(
                         writeIRepositoryType,
                         sp => Activator.CreateInstance(writeRepositoryType, dbContext, sp)!,
                         serviceLifetime
                    ));
                    services.Add(new ServiceDescriptor(
                        readIRepositoryType,
                        sp => Activator.CreateInstance(readRepositoryType, dbContext, sp)!,
                        serviceLifetime
                    ));
                    services.Add(new ServiceDescriptor(
                      dbReadIRepositoryType,
                      sp => Activator.CreateInstance(dbReadRepositoryType, dbContext, sp)!,
                      serviceLifetime
                    ));
                    services.Add(new ServiceDescriptor(
                      dbWriteIRepositoryType,
                      sp => Activator.CreateInstance(dbWriteRepositoryType, dbContext, sp)!,
                      serviceLifetime
                    ));
                    services.Add(new ServiceDescriptor(
                           iUnitOfWork,
                           sp =>
                           {
                               var repositoryFactory = (RepositoryNoneStaticFactory<TDbContext>)sp.GetRequiredService(typeof(RepositoryNoneStaticFactory<TDbContext>));

                               var createUnitOfWorkMethod = repositoryFactory
                                   .GetType()
                                   .GetMethod(nameof(RepositoryNoneStaticFactory<TDbContext>.CreateUnitOfWork), new[] { dbContext.GetType() });

                               if (createUnitOfWorkMethod == null)
                               {
                                   throw new InvalidOperationException("CreateUnitOfWork method is not found.");
                               }

                               var genericMethod = createUnitOfWorkMethod.MakeGenericMethod(entityType);

                               return genericMethod.Invoke(repositoryFactory, new object[] { dbContext });
                           },
                           serviceLifetime
                       ));
                }
            }
            return services;
        }
    }
}
