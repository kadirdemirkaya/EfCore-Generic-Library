using Base.Repository.Abstractions;
using Base.Repository.Options;
using EfCore.Repository.Abstractions;
using EfCore.Repository.Concretes;
using EfCore.Repository.Factory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Linq.Expressions;
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
        /// The first call also registers the entity-independent <see cref="IUnitOfWork"/> on that context; later calls keep
        /// the existing <see cref="IUnitOfWork"/> registration.
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
            services.TryAdd(new ServiceDescriptor(typeof(IUnitOfWork), sp => new UnitOfWork(sp.GetRequiredService<TDbContext>()), serviceLifetime));

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
                TDbContext dbContext = GetOrCreateSharedContext<TDbContext>(services);

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

                    if (HasContextConstructor<TDbContext>(entityType))
                    {
                        services.Add(new ServiceDescriptor(typeof(TBaseType), CreateContextFactory<TDbContext>(entityType), serviceLifetime));
                    }

                    services.Add(new ServiceDescriptor(baseReadIRepositoryType, CreateContextFactory<TDbContext>(baseReadRepositoryType), serviceLifetime));
                    services.Add(new ServiceDescriptor(baseWriteIRepositoryType, CreateContextFactory<TDbContext>(baseWriteRepositoryType), serviceLifetime));
                    services.Add(new ServiceDescriptor(writeIRepositoryType, CreateContextFactory<TDbContext>(writeRepositoryType), serviceLifetime));
                    services.Add(new ServiceDescriptor(readIRepositoryType, CreateContextFactory<TDbContext>(readRepositoryType), serviceLifetime));
                    services.Add(new ServiceDescriptor(dbReadIRepositoryType, CreateContextFactory<TDbContext>(dbReadRepositoryType), serviceLifetime));
                    services.Add(new ServiceDescriptor(dbWriteIRepositoryType, CreateContextFactory<TDbContext>(dbWriteRepositoryType), serviceLifetime));
                    services.Add(new ServiceDescriptor(iUnitOfWork, CreateUnitOfWorkFactory<TDbContext>(entityType), serviceLifetime));
                }
            }
            return services;
        }

        private static Func<IServiceProvider, object> CreateContextFactory<TDbContext>(Type implementationType)
            where TDbContext : DbContext
        {
            ConstructorInfo? constructor = implementationType.GetConstructor(new[] { typeof(TDbContext), typeof(IServiceProvider) });

            if (constructor is null)
            {
                return sp => Activator.CreateInstance(implementationType, sp.GetRequiredService<TDbContext>(), sp)!;
            }

            ParameterExpression contextParameter = Expression.Parameter(typeof(TDbContext), "dbContext");
            ParameterExpression providerParameter = Expression.Parameter(typeof(IServiceProvider), "serviceProvider");
            ParameterInfo[] constructorParameters = constructor.GetParameters();

            Func<TDbContext, IServiceProvider, object> create = Expression.Lambda<Func<TDbContext, IServiceProvider, object>>(
                Expression.Convert(
                    Expression.New(
                        constructor,
                        Expression.Convert(contextParameter, constructorParameters[0].ParameterType),
                        Expression.Convert(providerParameter, constructorParameters[1].ParameterType)),
                    typeof(object)),
                contextParameter,
                providerParameter).Compile();

            return sp =>
            {
                TDbContext dbContext = sp.GetRequiredService<TDbContext>();

                try
                {
                    return create(dbContext, sp);
                }
                catch (Exception exception)
                {
                    throw new TargetInvocationException(exception);
                }
            };
        }

        private static Func<IServiceProvider, object> CreateUnitOfWorkFactory<TDbContext>(Type entityType)
            where TDbContext : DbContext
        {
            MethodInfo? createUnitOfWorkMethod = typeof(RepositoryNoneStaticFactory<TDbContext>)
                .GetMethod(nameof(RepositoryNoneStaticFactory<TDbContext>.CreateUnitOfWork), new[] { typeof(TDbContext) });

            if (createUnitOfWorkMethod is null)
            {
                return _ => throw new InvalidOperationException("CreateUnitOfWork method is not found.");
            }

            ParameterExpression factoryParameter = Expression.Parameter(typeof(RepositoryNoneStaticFactory<TDbContext>), "repositoryFactory");
            ParameterExpression contextParameter = Expression.Parameter(typeof(TDbContext), "dbContext");

            Func<RepositoryNoneStaticFactory<TDbContext>, TDbContext, object> create = Expression.Lambda<Func<RepositoryNoneStaticFactory<TDbContext>, TDbContext, object>>(
                Expression.Convert(
                    Expression.Call(factoryParameter, createUnitOfWorkMethod.MakeGenericMethod(entityType), contextParameter),
                    typeof(object)),
                factoryParameter,
                contextParameter).Compile();

            return sp =>
            {
                TDbContext dbContext = sp.GetRequiredService<TDbContext>();

                var repositoryFactory = (RepositoryNoneStaticFactory<TDbContext>)sp.GetRequiredService(typeof(RepositoryNoneStaticFactory<TDbContext>));

                try
                {
                    return create(repositoryFactory, dbContext);
                }
                catch (Exception exception)
                {
                    throw new TargetInvocationException(exception);
                }
            };
        }

        private static bool IsConstructableEntity(Type type)
            => type.IsClass
               && !type.IsAbstract
               && !type.IsGenericTypeDefinition
               && type.GetConstructor(Type.EmptyTypes) != null;

        private static bool HasContextConstructor<TDbContext>(Type type)
            where TDbContext : DbContext
            => type.GetConstructor(new[] { typeof(TDbContext), typeof(IServiceProvider) }) != null;

        private static TDbContext GetOrCreateSharedContext<TDbContext>(IServiceCollection services)
            where TDbContext : DbContext
        {
            if (services.FirstOrDefault(d => d.ServiceType == typeof(SharedDbContext<TDbContext>))?.ImplementationInstance is SharedDbContext<TDbContext> shared)
            {
                return shared.Context;
            }

            TDbContext dbContext = (TDbContext)Activator.CreateInstance(typeof(TDbContext))!;
            services.Add(new ServiceDescriptor(typeof(SharedDbContext<TDbContext>), new SharedDbContext<TDbContext>(dbContext)));

            return dbContext;
        }

        private sealed class SharedDbContext<TDbContext>
            where TDbContext : DbContext
        {
            public SharedDbContext(TDbContext context) => Context = context;

            public TDbContext Context { get; }
        }
    }
}
