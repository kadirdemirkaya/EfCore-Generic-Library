using Base.Repository.Abstractions;
using Base.Repository.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace EfCore.Repository.Concretes
{
    public class BaseWriteRepository<TEntity> : IBaseWriteRepository<TEntity>
        where TEntity : class, new()
    {
        private readonly DbContext _dbContext;
        public IServiceProvider? _serviceProvider { get; set; }
        public DatabaseOptions? _databaseOptions { get; set; }

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

        /// <inheritdoc/>
        public async Task<bool> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default)
         => await _dbContext.Database.ExecuteSqlRawAsync(command, cancellationToken).ConfigureAwait(false) > 0;

        /// <inheritdoc/>
        public async Task<bool> ExecuteCommandAsync(string command, params object[] parameters)
            => await _dbContext.Database.ExecuteSqlRawAsync(command, parameters).ConfigureAwait(false) > 0;

        /// <inheritdoc/>
        public async Task<bool> ExecuteCommandAsync(string command, IEnumerable<object> parameters, CancellationToken cancellationToken = default)
            => await _dbContext.Database.ExecuteSqlRawAsync(command, parameters, cancellationToken).ConfigureAwait(false) > 0;

        /// <summary>
        /// Runs an interpolated SQL command (for example an <c>UPDATE</c> or <c>DELETE</c>) on the repository's context.
        /// Every interpolated value is sent as a database parameter, never inlined into the SQL text, so a value cannot
        /// change the command; <see langword="null"/> values are sent as database <c>NULL</c>. The command runs immediately
        /// and bypasses the change tracker, and it joins the context's current transaction, so a rollback undoes it.
        /// </summary>
        /// <param name="command">The SQL as an interpolated string, for example <c>$"UPDATE Persons SET Age = {age} WHERE Id = {id}"</c>.</param>
        /// <param name="cancellationToken">A token to cancel the command.</param>
        /// <returns><see langword="true"/> when at least one row was affected; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="command"/> is <see langword="null"/>.</exception>
        public async Task<bool> ExecuteCommandInterpolatedAsync(FormattableString command, CancellationToken cancellationToken = default)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            return await _dbContext.Database.ExecuteSqlInterpolatedAsync(command, cancellationToken).ConfigureAwait(false) > 0;
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.Unspecified,
        CancellationToken cancellationToken = default)
        {
            if (isolationLevel == IsolationLevel.Unspecified)
            {
                return await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            }

            return await _dbContext.Database.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            EntityUpdate.MarkModified(_dbContext, entity);

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
            => await _dbContext.SaveChangesAsync().ConfigureAwait(false) > 0;

        /// <summary>
        /// Writes every pending change of the repository's context.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> when at least one row was written; otherwise <see langword="false"/>.</returns>
        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken)
            => await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;
    }
}
