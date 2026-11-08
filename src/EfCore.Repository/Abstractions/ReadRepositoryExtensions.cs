using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace EfCore.Repository.Abstractions
{
    /// <summary>
    /// Read operations for any <see cref="IReadRepository{TEntity}"/>, including the repositories returned by a unit of
    /// work: queries whose rows are streamed one at a time instead of being loaded into a list.
    /// </summary>
    public static class ReadRepositoryExtensions
    {
        /// <summary>
        /// Streams the rows selected by <paramref name="specification"/> one at a time while the caller enumerates them,
        /// without loading the whole result into memory. Conditions, includes, ordering, <c>Skip</c>/<c>Take</c> and the
        /// query options of the specification apply exactly as in <c>GetListAsync(Specification)</c>; without an
        /// <see cref="SpecificationBase{TEntity}.OrderBy"/> the row order is whatever the database returns. The query runs
        /// when the enumeration starts, not when this method is called, and keeps the context's connection busy until the
        /// enumeration ends, so do not run other queries on the same context inside the loop. Tracked results stay in the
        /// change tracker; set <see cref="SpecificationBase{TEntity}.AsNoTracking"/> for large streams. Cancelling
        /// <paramref name="cancellationToken"/> or a token passed with <c>WithCancellation</c> stops the enumeration with an
        /// <see cref="OperationCanceledException"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="repository">The repository whose context runs the query.</param>
        /// <param name="specification">The specification that selects, shapes and orders the rows.</param>
        /// <param name="cancellationToken">A token to cancel the enumeration.</param>
        /// <returns>The rows, in the order of the query.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="specification"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TEntity> StreamAsync<TEntity>(
            this IReadRepository<TEntity> repository,
            Specification<TEntity> specification,
            CancellationToken cancellationToken = default)
            where TEntity : class
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            if (specification == null)
            {
                throw new ArgumentNullException(nameof(specification));
            }

            return Enumerate(repository.Table.Set<TEntity>().GetSpecifiedQuery(specification), cancellationToken);
        }

        /// <summary>
        /// Streams the rows of <typeparamref name="TEntity"/> that match <paramref name="filter"/> one at a time while the
        /// caller enumerates them, with the same behaviour as the specification overload of <c>StreamAsync</c>. The row
        /// order is whatever the database returns; use the specification overload to order the rows.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="repository">The repository whose context runs the query.</param>
        /// <param name="filter">The condition the rows must match.</param>
        /// <param name="asNoTracking"><see langword="true"/> to return the rows without tracking them.</param>
        /// <param name="cancellationToken">A token to cancel the enumeration.</param>
        /// <returns>The matching rows.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="filter"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TEntity> StreamAsync<TEntity>(
            this IReadRepository<TEntity> repository,
            Expression<Func<TEntity, bool>> filter,
            bool asNoTracking = false,
            CancellationToken cancellationToken = default)
            where TEntity : class
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            IQueryable<TEntity> query = repository.Table.Set<TEntity>().Where(filter);

            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            return Enumerate(query, cancellationToken);
        }

        private static async IAsyncEnumerable<TEntity> Enumerate<TEntity>(
            IQueryable<TEntity> query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await foreach (TEntity entity in query.AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return entity;
            }
        }
    }
}
