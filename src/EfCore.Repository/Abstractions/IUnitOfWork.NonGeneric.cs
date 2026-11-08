using System.Data;

namespace EfCore.Repository.Abstractions
{
    /// <summary>
    /// A unit of work over one context that is not bound to an entity type: repositories of any entity type share the
    /// context, so their changes are written together by one <see cref="SaveChangesAsync"/> call or one transaction.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Returns a read repository of <typeparamref name="TEntity"/> on the unit of work's context.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <returns>A read repository that uses the unit of work's context.</returns>
        IReadRepository<TEntity> GetReadRepository<TEntity>()
            where TEntity : class, new();

        /// <summary>
        /// Returns a write repository of <typeparamref name="TEntity"/> on the unit of work's context.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <returns>A write repository that uses the unit of work's context.</returns>
        IWriteRepository<TEntity> GetWriteRepository<TEntity>()
            where TEntity : class, new();

        /// <summary>
        /// Writes every pending change of the context, whatever entity type it belongs to, in one call.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> when at least one row was written; otherwise <see langword="false"/>.</returns>
        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs <paramref name="operation"/> in a database transaction through the provider's execution strategy, so a
        /// configured retry strategy re-runs the whole operation. After the operation, pending changes are saved and the
        /// transaction is committed. When the operation, the save or the commit fails, the transaction is rolled back,
        /// the context's change tracker is cleared and the exception is rethrown, so nothing of the attempt is written.
        /// When the context already has an active transaction, the operation joins it: pending changes are saved, but
        /// committing or rolling back stays with the owner of that transaction.
        /// </summary>
        /// <param name="operation">The work to run; receives <paramref name="cancellationToken"/>. With a retry strategy it can run more than once.</param>
        /// <param name="isolationLevel">The isolation level of the transaction; <see cref="IsolationLevel.Unspecified"/> uses the provider default.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that completes when the transaction has been committed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            IsolationLevel isolationLevel = IsolationLevel.Unspecified,
            CancellationToken cancellationToken = default);
    }
}
