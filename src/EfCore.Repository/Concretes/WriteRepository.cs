using Base.Repository.Options;
using EfCore.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Data;

namespace EfCore.Repository.Concretes
{
    public class WriteRepository<TEntity> : BaseWriteRepository<TEntity>, IWriteRepository<TEntity>
        where TEntity : class, new()
    {
        public DbContext _dbContext { get; set; }
        public IServiceProvider _serviceProvider { get; set; }
        public DatabaseOptions _databaseOptions { get; set; }

        public WriteRepository(DbContext dbContext)
            : base(dbContext)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(_dbContext), "DatabaseOptions cannot be null");
            }
            _dbContext = dbContext;
        }

        public WriteRepository(DatabaseOptions databaseOptions, IServiceProvider serviceProvider)
            : base(databaseOptions, serviceProvider)
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

        public WriteRepository(DbContext dbContext, IServiceProvider serviceProvider)
            : base(dbContext, serviceProvider)
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

        public void Add(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            _dbContext.Set<TEntity>().Add(entity);
        }

        public void Add(IEnumerable<TEntity> entities)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            _dbContext.Set<TEntity>().AddRange(entities);
        }

        public async Task AddAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            await _dbContext.Set<TEntity>().AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> DeleteAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {

            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            _dbContext.Set<TEntity>().RemoveRange(entities);

            return await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;

        }


        public async Task<object[]> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            EntityEntry<TEntity> entityEntry = await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            object[] primaryKeyValue = entityEntry.Metadata.FindPrimaryKey().Properties.
                Select(p => entityEntry.Property(p.Name).CurrentValue).ToArray();

            return primaryKeyValue;
        }

        public async Task<object[]> InsertAsync(List<TEntity> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null || entities.Count() == 0)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            var primaryKeyValues = new List<object[]>();

            foreach (var entity in entities)
            {
                EntityEntry<TEntity> entityEntry = await _dbContext.Set<TEntity>().AddAsync(entity, cancellationToken).ConfigureAwait(false);
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                primaryKeyValues.Add(entityEntry.Metadata.FindPrimaryKey().Properties.
                    Select(p => entityEntry.Property(p.Name).CurrentValue).ToArray());
            }

            return primaryKeyValues.ToArray();
        }

        public async Task InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            await _dbContext.Set<TEntity>().AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Remove(IEnumerable<TEntity> entities)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            _dbContext.Set<TEntity>().RemoveRange(entities);
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;

        public async Task<int> SaveChangesCountAsync(CancellationToken cancellationToken = default)
         => await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        public async Task<bool> UpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            _dbContext.Set<TEntity>().UpdateRange(entities);
            return await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;
        }

    }
}
