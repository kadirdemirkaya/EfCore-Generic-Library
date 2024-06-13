using Base.Repository.Abstractions;
using Base.Repository.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace EfCore.Repository.Concretes
{
    public class BaseWriteRepository<TEntity> : IBaseWriteRepository<TEntity>
        where TEntity : class, new()
    {
        public DbContext _dbContext { get; set; }
        public IServiceProvider _serviceProvider { get; set; }
        public DatabaseOptions _databaseOptions { get; set; }

        public BaseWriteRepository(DbContext dbContext)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(_dbContext), "DatabaseOptions cannot be null");
            }
            _dbContext = dbContext;
        }

        public BaseWriteRepository(DatabaseOptions databaseOptions, IServiceProvider serviceProvider)
        {
            if (databaseOptions == null)
            {
                throw new ArgumentNullException(nameof(databaseOptions), "DatabaseOptions cannot be null");
            }
            _databaseOptions = databaseOptions;
            if (databaseOptions.Connection == null)
            {
                throw new ArgumentNullException(nameof(databaseOptions.Connection), "DatabaseOptions.Connection cannot be null");
            }
            _serviceProvider = serviceProvider;
            _dbContext = (DbContext)databaseOptions.Connection;
        }

        public BaseWriteRepository(DbContext dbContext, IServiceProvider serviceProvider)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(_dbContext), "DatabaseOptions cannot be null");
            }
            _dbContext = dbContext;
            _serviceProvider = serviceProvider;
        }

        public DbContext Table
        {
            get
            {
                return _dbContext;
            }
        }

        public async Task<bool> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default)
         => await _dbContext.Database.ExecuteSqlRawAsync(command, cancellationToken) > 0;

        public async Task<bool> ExecuteCommandAsync(string command, params object[] parameters)
            => await _dbContext.Database.ExecuteSqlRawAsync(command, parameters) > 0;

        public async Task<bool> ExecuteCommandAsync(string command, IEnumerable<object> parameters, CancellationToken cancellationToken = default)
            => await _dbContext.Database.ExecuteSqlRawAsync(command, parameters, cancellationToken) > 0;

        public async Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.Unspecified,
        CancellationToken cancellationToken = default)
          => await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        public async Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            EntityEntry<TEntity> trackedEntity = _dbContext.ChangeTracker.Entries<TEntity>().FirstOrDefault(x => x.Entity == entity);

            if (trackedEntity == null)
            {
                IEntityType entityType = _dbContext.Model.FindEntityType(typeof(TEntity));

                if (entityType == null)
                {
                    throw new InvalidOperationException($"{typeof(TEntity).Name} is not part of EF Core DbContext model");
                }

                string primaryKeyName = entityType.FindPrimaryKey().Properties.Select(p => p.Name).FirstOrDefault();

                if (primaryKeyName != null)
                {
                    Type primaryKeyType = entityType.FindPrimaryKey().Properties.Select(p => p.ClrType).FirstOrDefault();

                    object primaryKeyDefaultValue = primaryKeyType.IsValueType ? Activator.CreateInstance(primaryKeyType) : null;

                    object primaryValue = entity.GetType().GetProperty(primaryKeyName).GetValue(entity, null);

                    if (primaryKeyDefaultValue.Equals(primaryValue))
                    {
                        throw new InvalidOperationException("The primary key value of the entity to be updated is not valid.");
                    }
                }

                _dbContext.Set<TEntity>().Update(entity);
            }

            return await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;
        }

        public async Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            _dbContext.Set<TEntity>().Remove(entity);
            return await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;
        }

        public void Remove(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            _dbContext.Set<TEntity>().Remove(entity);
        }
        public void ClearChangeTracker()
        {
            _dbContext.ChangeTracker.Clear();
        }

        public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> SaveChangesAsync()
            => await _dbContext.SaveChangesAsync() > 0;
    }
}
