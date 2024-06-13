using Base.Repository.Abstractions;
using Base.Repository.Options;
using EfCore.Repository.Abstractions;
using EfCore.Repository.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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

        public void IsValid(ParameterValidationAttribute attribute, MethodInfo method, params object[] parameters)
        {
            if (attribute == null)
            {
                throw new InvalidOperationException("ParameterValidationAttribute is not applied to the method.");
            }

            attribute.Validate(method, parameters);
        }

        public ITable GetTable() => new Table(_dbContext);


        public IBaseReadRepository<TEntity> GetBaseReadRepository() => _serviceProvider.GetRequiredService<IBaseReadRepository<TEntity>>();

        // public IBaseReadRepository<TEntity> GetBaseReadRepository() => new BaseReadRepository<TEntity>(_dbContext);

        //[ParameterValidation(true)]
        //public IBaseReadRepository<TEntity> GetBaseReadRepository(bool isServiceProvider)
        //{
        //    var method = (MethodInfo)MethodBase.GetCurrentMethod();
        //    var attribute = (ParameterValidationAttribute)method.GetCustomAttribute(typeof(ParameterValidationAttribute));

        //    IsValid(attribute, method, isServiceProvider);

        //    return _serviceProvider.GetRequiredService<IBaseReadRepository<TEntity>>();
        //    //return new BaseReadRepository<TEntity>(_dbContext, _serviceProvider);
        //}

        //[ParameterValidation(true, true)]
        //public IBaseReadRepository<TEntity> GetBaseReadRepository(bool isDatabaseOptions, bool isServiceProvider)
        //{
        //    var method = (MethodInfo)MethodBase.GetCurrentMethod();
        //    var attribute = (ParameterValidationAttribute)method.GetCustomAttribute(typeof(ParameterValidationAttribute));

        //    IsValid(attribute, method, isDatabaseOptions, isServiceProvider);


        //    return _serviceProvider.GetRequiredService<IBaseReadRepository<TEntity>>();
        //    //return new BaseReadRepository<TEntity>(_databaseOptions, _serviceProvider);
        //}



        public IBaseWriteRepository<TEntity> GetBaseWriteRepository() => _serviceProvider.GetRequiredService<IBaseWriteRepository<TEntity>>();

        //[ParameterValidation(true)]
        //public IBaseWriteRepository<TEntity> GetBaseWriteRepository(bool isServiceProvider)
        //{
        //    var method = (MethodInfo)MethodBase.GetCurrentMethod();
        //    var attribute = (ParameterValidationAttribute)method.GetCustomAttribute(typeof(ParameterValidationAttribute));
        //    IsValid(attribute, method, isServiceProvider);

        //    return _serviceProvider.GetRequiredService<IBaseWriteRepository<TEntity>>();
        //    //return new BaseWriteRepository<TEntity>(_dbContext, _serviceProvider);
        //}

        //[ParameterValidation(true, true)]
        //public IBaseWriteRepository<TEntity> GetBaseWriteRepository(bool isDatabaseOptions, bool isServiceProvider)
        //{
        //    var method = (MethodInfo)MethodBase.GetCurrentMethod();
        //    var attribute = (ParameterValidationAttribute)method.GetCustomAttribute(typeof(ParameterValidationAttribute));
        //    IsValid(attribute, method, isDatabaseOptions, isServiceProvider);

        //    return _serviceProvider.GetRequiredService<IBaseWriteRepository<TEntity>>();
        //    //return new BaseWriteRepository<TEntity>(_databaseOptions, _serviceProvider); ;
        //}



        public IDbReadRepository<TEntity> GetDbReadRepository() => _serviceProvider.GetRequiredService<IDbReadRepository<TEntity>>();

        //[ParameterValidation(true)]
        //public IDbReadRepository<TEntity> GetDbReadRepository(bool isServiceProvider)
        //{
        //    var method = (MethodInfo)MethodBase.GetCurrentMethod();
        //    var attribute = (ParameterValidationAttribute)method.GetCustomAttribute(typeof(ParameterValidationAttribute));
        //    IsValid(attribute, method, isServiceProvider);

        //    return _serviceProvider.GetRequiredService<IDbReadRepository<TEntity>>();
        //    //return new DbReadRepository<TEntity>(_dbContext, _serviceProvider);
        //}

        //[ParameterValidation(true, true)]
        //public IDbReadRepository<TEntity> GetDbReadRepository(bool isDatabaseOptions, bool isServiceProvider)
        //{
        //    var method = (MethodInfo)MethodBase.GetCurrentMethod();
        //    var attribute = (ParameterValidationAttribute)method.GetCustomAttribute(typeof(ParameterValidationAttribute));
        //    IsValid(attribute, method, isDatabaseOptions, isServiceProvider);

        //    return _serviceProvider.GetRequiredService<IDbReadRepository<TEntity>>();
        //    //return new DbReadRepository<TEntity>(_databaseOptions, _serviceProvider);
        //}


        public IDbWriteRepository<TEntity> GetDbWriteRepository() => _serviceProvider.GetRequiredService<IDbWriteRepository<TEntity>>();


        //[ParameterValidation(true)]
        //public IDbWriteRepository<TEntity> GetDbWriteRepository(bool isServiceProvider)
        //{
        //    var method = (MethodInfo)MethodBase.GetCurrentMethod();
        //    var attribute = (ParameterValidationAttribute)method.GetCustomAttribute(typeof(ParameterValidationAttribute));
        //    IsValid(attribute, method, isServiceProvider);

        //    return _serviceProvider.GetRequiredService<IDbWriteRepository<TEntity>>();
        //    //return new DbWriteRepository<TEntity>(_dbContext, _serviceProvider); ;
        //}

        //[ParameterValidation(true, true)]
        //public IDbWriteRepository<TEntity> GetDbWriteRepository(bool isDatabaseOptions, bool isServiceProvider)
        //{
        //    var method = (MethodInfo)MethodBase.GetCurrentMethod();
        //    var attribute = (ParameterValidationAttribute)method.GetCustomAttribute(typeof(ParameterValidationAttribute));
        //    IsValid(attribute, method, isDatabaseOptions, isServiceProvider);

        //    return _serviceProvider.GetRequiredService<IDbWriteRepository<TEntity>>();
        //    //return new DbWriteRepository<TEntity>(_databaseOptions, _serviceProvider);
        //}



        public IReadRepository<TEntity> GetReadRepository() => _serviceProvider.GetRequiredService<IReadRepository<TEntity>>();

        //[ParameterValidation(true)]
        //public IReadRepository<TEntity> GetReadRepository(bool isServiceProvider)
        //{
        //    var method = (MethodInfo)MethodBase.GetCurrentMethod();
        //    var attribute = (ParameterValidationAttribute)method.GetCustomAttribute(typeof(ParameterValidationAttribute));
        //    IsValid(attribute, method, isServiceProvider);

        //    return _serviceProvider.GetRequiredService<IReadRepository<TEntity>>();
        //    //return new ReadRepository<TEntity>(_dbContext, _serviceProvider);
        //}

        //[ParameterValidation(true, true)]
        //public IReadRepository<TEntity> GetReadRepository(bool isDatabaseOptions, bool isServiceProvider)
        //{
        //    var method = (MethodInfo)MethodBase.GetCurrentMethod();
        //    var attribute = (ParameterValidationAttribute)method.GetCustomAttribute(typeof(ParameterValidationAttribute));
        //    IsValid(attribute, method, isDatabaseOptions, isServiceProvider);

        //    return _serviceProvider.GetRequiredService<IReadRepository<TEntity>>();
        //    //return new ReadRepository<TEntity>(_databaseOptions, _serviceProvider);
        //}


        public IWriteRepository<TEntity> GetWriteRepository() => _serviceProvider.GetRequiredService<IWriteRepository<TEntity>>();


        //[ParameterValidation(true)]
        //public IWriteRepository<TEntity> GetWriteRepository(bool isServiceProvider)
        //{
        //    var method = (MethodInfo)MethodBase.GetCurrentMethod();
        //    var attribute = (ParameterValidationAttribute)method.GetCustomAttribute(typeof(ParameterValidationAttribute));
        //    IsValid(attribute, method, isServiceProvider);

        //    return _serviceProvider.GetRequiredService<IWriteRepository<TEntity>>();
        //    //return new WriteRepository<TEntity>(_dbContext, _serviceProvider);
        //}

        //[ParameterValidation(true, true)]
        //public IWriteRepository<TEntity> GetWriteRepository(bool isDatabaseOptions, bool isServiceProvider)
        //{
        //    var method = (MethodInfo)MethodBase.GetCurrentMethod();
        //    var attribute = (ParameterValidationAttribute)method.GetCustomAttribute(typeof(ParameterValidationAttribute));
        //    IsValid(attribute, method, isDatabaseOptions, isServiceProvider);

        //    return _serviceProvider.GetRequiredService<IWriteRepository<TEntity>>();
        //    //return new WriteRepository<TEntity>(_databaseOptions, _serviceProvider);
        //}



        public async Task<bool> SaveChangesAsync() => await _dbContext.SaveChangesAsync() > 0;
    }
}
