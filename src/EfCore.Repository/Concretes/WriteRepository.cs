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
        private readonly DbContext _dbContext;
        public new IServiceProvider? _serviceProvider { get; set; }
        public new DatabaseOptions? _databaseOptions { get; set; }

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

        public new DbContext Table
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

            object[] primaryKeyValue = entityEntry.Metadata.FindPrimaryKey()!.Properties.
                Select(p => entityEntry.Property(p.Name).CurrentValue!).ToArray();

            return primaryKeyValue;
        }

        public async Task<object[]> InsertAsync(List<TEntity> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null || entities.Count() == 0)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            if (entities.Any(entity => entity == null))
            {
                throw new ArgumentNullException(nameof(entities), "The list cannot contain null entities.");
            }

            await _dbContext.Set<TEntity>().AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var primaryKeyValues = new List<object[]>(entities.Count);

            foreach (var entity in entities)
            {
                EntityEntry<TEntity> entityEntry = _dbContext.Entry(entity);
                primaryKeyValues.Add(entityEntry.Metadata.FindPrimaryKey()!.Properties.
                    Select(p => entityEntry.Property(p.Name).CurrentValue!).ToArray());
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

        public new async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
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

        /// <summary>
        /// Marks <paramref name="entity"/> as modified without saving; the change is written by the next
        /// <c>SaveChangesAsync</c> of the repository or of the unit of work sharing its context. A tracked entity is left
        /// as it is, so only its changed properties are written. An untracked entity is attached with every property
        /// marked modified after the same primary key check as <c>UpdateAsync</c>.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <exception cref="ArgumentNullException"><paramref name="entity"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The entity is untracked and a primary key part is <see langword="null"/> or every part holds its default value.</exception>
        public void Update(TEntity entity)
            => EntityUpdate.MarkModified(_dbContext, entity);

        /// <summary>
        /// Marks every entity of <paramref name="entities"/> as modified without saving, with the same rules as
        /// <see cref="Update(TEntity)"/>. Every untracked entity is checked before any of them is attached, so an invalid
        /// key leaves the context unchanged. One <c>SaveChangesAsync</c> writes them all.
        /// </summary>
        /// <param name="entities">The entities to update.</param>
        /// <exception cref="ArgumentNullException"><paramref name="entities"/> is <see langword="null"/> or contains <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">An untracked entity has a primary key part that is <see langword="null"/> or only default key values.</exception>
        public void Update(IEnumerable<TEntity> entities)
            => EntityUpdate.MarkModified(_dbContext, entities);

    }
}
