using Base.Repository.Abstractions;
using Base.Repository.Options;
using EfCore.Repository.Abstractions;
using EfCore.Repository.Concretes;
using EfCore.Repository.Factory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using System.Reflection.Metadata;

namespace EfCore.Repository.Extensions
{
    public static class EfCoreRepositoryExtension
    {
        /// <summary>
        /// Registers the repositories and the unit of work of <typeparamref name="TEntity"/>.
        /// The <typeparamref name="TDbContext"/> is resolved from the container on every resolve, so the
        /// configuration and lifetime given to <c>AddDbContext</c> apply and each scope works on its own context.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TDbContext">The context type, registered with <c>AddDbContext</c>.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="serviceLifetime">The lifetime of the registered repositories and unit of work.</param>
        /// <returns>The same service collection.</returns>
        public static IServiceCollection AddEfCoreRepository<TEntity, TDbContext>(this IServiceCollection services, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TEntity : class, new()
            where TDbContext : DbContext
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Add(new ServiceDescriptor(typeof(IBaseReadRepository<TEntity>), sp => new BaseReadRepository<TEntity>(sp.GetRequiredService<TDbContext>(), sp), serviceLifetime));
            services.Add(new ServiceDescriptor(typeof(IBaseWriteRepository<TEntity>), sp => new BaseWriteRepository<TEntity>(sp.GetRequiredService<TDbContext>(), sp), serviceLifetime));
            services.Add(new ServiceDescriptor(typeof(IReadRepository<TEntity>), sp => new ReadRepository<TEntity>(sp.GetRequiredService<TDbContext>(), sp), serviceLifetime));
            services.Add(new ServiceDescriptor(typeof(IWriteRepository<TEntity>), sp => new WriteRepository<TEntity>(sp.GetRequiredService<TDbContext>(), sp), serviceLifetime));
            services.Add(new ServiceDescriptor(typeof(IDbReadRepository<TEntity>), sp => new DbReadRepository<TEntity>(sp.GetRequiredService<TDbContext>(), sp), serviceLifetime));
            services.Add(new ServiceDescriptor(typeof(IDbWriteRepository<TEntity>), sp => new DbWriteRepository<TEntity>(sp.GetRequiredService<TDbContext>(), sp), serviceLifetime));
            services.Add(new ServiceDescriptor(typeof(IUnitOfWork<TEntity>), sp => RepositoryFactory<TDbContext>.CreateUnitOfWork<TEntity>(sp.GetRequiredService<TDbContext>(), sp), serviceLifetime));

            return services;
        }

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
                 sp => RepositoryFactory<TDbContext>.CreateUnitOfWork<TEntity>(databaseOptions, sp),
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
            services.TryAdd(new ServiceDescriptor(typeof(RepositoryNoneStaticFactory<>), typeof(RepositoryNoneStaticFactory<>), ServiceLifetime.Scoped));

            foreach (var assembly in assemblies)
            {
                var entityTypes = assembly.GetTypes()
                                          .Where(t => typeof(TBaseType).IsAssignableFrom(t) && IsConstructableEntity(t))
                                          .ToList();

                foreach (var entityType in entityTypes)
                {
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

                    services.Add(new ServiceDescriptor(
                        typeof(TBaseType),
                        sp =>
                        {
                            var dbContext = sp.GetRequiredService<TDbContext>();

                            return Activator.CreateInstance(entityType, dbContext, sp)!;
                        },
                        serviceLifetime
                    ));

                    services.Add(new ServiceDescriptor(
                        baseReadIRepositoryType,
                        sp =>
                        {
                            var dbContext = sp.GetRequiredService<TDbContext>();

                            return Activator.CreateInstance(baseReadRepositoryType, dbContext, sp)!;
                        },
                        serviceLifetime
                    ));
                    services.Add(new ServiceDescriptor(
                        baseWriteIRepositoryType,
                        sp =>
                        {
                            var dbContext = sp.GetRequiredService<TDbContext>();

                            return Activator.CreateInstance(baseWriteRepositoryType, dbContext, sp)!;
                        },
                        serviceLifetime
                    ));
                    services.Add(new ServiceDescriptor(
                         writeIRepositoryType,
                         sp =>
                         {
                             var dbContext = sp.GetRequiredService<TDbContext>();

                             return Activator.CreateInstance(writeRepositoryType, dbContext, sp)!;
                         },
                         serviceLifetime
                    ));
                    services.Add(new ServiceDescriptor(
                        readIRepositoryType,
                        sp =>
                        {
                            var dbContext = sp.GetRequiredService<TDbContext>();

                            return Activator.CreateInstance(readRepositoryType, dbContext, sp)!;
                        },
                        serviceLifetime
                    ));
                    services.Add(new ServiceDescriptor(
                      dbReadIRepositoryType,
                      sp =>
                      {
                          var dbContext = sp.GetRequiredService<TDbContext>();

                          return Activator.CreateInstance(dbReadRepositoryType, dbContext, sp)!;
                      },
                      serviceLifetime
                    ));
                    services.Add(new ServiceDescriptor(
                      dbWriteIRepositoryType,
                      sp =>
                      {
                          var dbContext = sp.GetRequiredService<TDbContext>();

                          return Activator.CreateInstance(dbWriteRepositoryType, dbContext, sp)!;
                      },
                      serviceLifetime
                    ));
                    services.Add(new ServiceDescriptor(
                           iUnitOfWork,
                           sp =>
                           {
                               var dbContext = sp.GetRequiredService<TDbContext>();

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

        private static bool IsConstructableEntity(Type type)
            => type.IsClass
               && !type.IsAbstract
               && !type.IsGenericTypeDefinition
               && type.GetConstructor(Type.EmptyTypes) != null;
    }
}
