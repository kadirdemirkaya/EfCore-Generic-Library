using EfCore.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace EfCore.Repository.Concretes
{
    /// <summary>
    /// The <see cref="IUnitOfWork"/> implementation: every repository it returns works on the context given to the constructor.
    /// </summary>
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _dbContext;

        /// <summary>
        /// Creates a unit of work on <paramref name="dbContext"/>.
        /// </summary>
        /// <param name="dbContext">The context shared by every repository of the unit of work.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dbContext"/> is <see langword="null"/>.</exception>
        public UnitOfWork(DbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <inheritdoc />
        public IReadRepository<TEntity> GetReadRepository<TEntity>()
            where TEntity : class, new()
            => new ReadRepository<TEntity>(_dbContext);

        /// <inheritdoc />
        public IWriteRepository<TEntity> GetWriteRepository<TEntity>()
            where TEntity : class, new()
            => new WriteRepository<TEntity>(_dbContext);

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;

        /// <inheritdoc />
        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            IsolationLevel isolationLevel = IsolationLevel.Unspecified,
            CancellationToken cancellationToken = default)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (_dbContext.Database.CurrentTransaction is not null)
            {
                return RunAndSaveAsync(operation, cancellationToken);
            }

            IExecutionStrategy strategy = _dbContext.Database.CreateExecutionStrategy();
            return strategy.ExecuteAsync(ct => RunInNewTransactionAsync(operation, isolationLevel, ct), cancellationToken);
        }

        private async Task RunAndSaveAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
        {
            await operation(cancellationToken).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task RunInNewTransactionAsync(Func<CancellationToken, Task> operation, IsolationLevel isolationLevel, CancellationToken cancellationToken)
        {
            await using IDbContextTransaction transaction = isolationLevel == IsolationLevel.Unspecified
                ? await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false)
                : await _dbContext.Database.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);

            try
            {
                await RunAndSaveAsync(operation, cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                _dbContext.ChangeTracker.Clear();
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }
    }
}
