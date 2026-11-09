using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace EfCore.Repository.Abstractions
{
    /// <summary>
    /// Read operations for any <see cref="IReadRepository{TEntity}"/>, including the repositories returned by a unit of
    /// work: queries whose rows are streamed one at a time instead of being loaded into a list, keyset (cursor)
    /// pagination by a single key, and row counts as <see cref="long"/>.
    /// </summary>
    public static class ReadRepositoryExtensions
    {
        /// <summary>
        /// Counts the rows of <typeparamref name="TEntity"/> that match <paramref name="filter"/> and returns the count as a
        /// <see cref="long"/>, for tables that can hold more than <see cref="int.MaxValue"/> rows. The count runs in the
        /// database (SQL <c>COUNT</c>); global query filters of the model apply as in <c>CountAsync</c>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="repository">The repository whose context runs the query.</param>
        /// <param name="filter">An optional condition the rows must match; <see langword="null"/> counts every row.</param>
        /// <param name="cancellationToken">A token to cancel the query.</param>
        /// <returns>The number of matching rows.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
        public static Task<long> LongCountAsync<TEntity>(
            this IReadRepository<TEntity> repository,
            Expression<Func<TEntity, bool>>? filter = null,
            CancellationToken cancellationToken = default)
            where TEntity : class
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            IQueryable<TEntity> query = repository.Table.Set<TEntity>();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return query.LongCountAsync(cancellationToken);
        }

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

        /// <summary>
        /// Reads one page of the rows of <typeparamref name="TEntity"/> that match <paramref name="filter"/> with keyset
        /// (cursor) pagination: the rows are ordered by the key chosen by <paramref name="keySelector"/> and the page starts
        /// right after the row whose key is <paramref name="after"/>, so the database seeks to the cursor instead of
        /// skipping rows, and rows inserted or deleted before the cursor do not shift later pages. Pass
        /// <see langword="null"/> for the first page and <see cref="KeysetPage{TEntity, TKey}.NextCursor"/> for the next.
        /// The key must be a single, unique, non-null column (for example the primary key or a unique column); with
        /// duplicate key values, rows sharing the cursor's key are skipped. One query reads <paramref name="pageSize"/>
        /// + 1 rows to find out whether more rows follow; no total count is computed.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TKey">The value type of the key, for example <see cref="int"/>, <see cref="long"/> or <see cref="DateTime"/>.</typeparam>
        /// <param name="repository">The repository whose context runs the query.</param>
        /// <param name="keySelector">The key the rows are ordered and paged by, for example <c>p =&gt; p.Id</c>.</param>
        /// <param name="after">The key of the last row of the previous page, or <see langword="null"/> for the first page.</param>
        /// <param name="pageSize">The maximum number of rows to return; must be greater than 0.</param>
        /// <param name="filter">An optional condition the rows must match; <see langword="null"/> pages every row.</param>
        /// <param name="descending"><see langword="true"/> to page from the largest key to the smallest.</param>
        /// <param name="asNoTracking"><see langword="true"/> to return the rows without tracking them.</param>
        /// <param name="cancellationToken">A token to cancel the query.</param>
        /// <returns>The page, whether more rows follow and the cursor of the next page.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> is less than 1.</exception>
        public static Task<KeysetPage<TEntity, TKey>> GetPageAfterAsync<TEntity, TKey>(
            this IReadRepository<TEntity> repository,
            Expression<Func<TEntity, TKey>> keySelector,
            TKey? after,
            int pageSize,
            Expression<Func<TEntity, bool>>? filter = null,
            bool descending = false,
            bool asNoTracking = false,
            CancellationToken cancellationToken = default)
            where TEntity : class
            where TKey : struct, IComparable<TKey>
        {
            IQueryable<TEntity> query = FilteredKeysetSource(repository, keySelector, pageSize, filter, asNoTracking);

            return ReadKeysetPageAsync(query, keySelector, after.HasValue, after.GetValueOrDefault(), pageSize, descending, cancellationToken);
        }

        /// <summary>
        /// Reads one page of the rows of <typeparamref name="TEntity"/> that match <paramref name="filter"/> with keyset
        /// (cursor) pagination by a reference-type key such as a <see cref="string"/>, with the same behaviour as the
        /// value-type overload of <c>GetPageAfterAsync</c>. Pass <see langword="null"/> for the first page and
        /// <see cref="KeysetPage{TEntity, TKey}.NextCursor"/> for the next. The rows are ordered and compared by the
        /// database, so its collation decides the order of strings.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TKey">The reference type of the key, for example <see cref="string"/>.</typeparam>
        /// <param name="repository">The repository whose context runs the query.</param>
        /// <param name="keySelector">The key the rows are ordered and paged by, for example <c>p =&gt; p.Code</c>.</param>
        /// <param name="after">The key of the last row of the previous page, or <see langword="null"/> for the first page.</param>
        /// <param name="pageSize">The maximum number of rows to return; must be greater than 0.</param>
        /// <param name="filter">An optional condition the rows must match; <see langword="null"/> pages every row.</param>
        /// <param name="descending"><see langword="true"/> to page from the largest key to the smallest.</param>
        /// <param name="asNoTracking"><see langword="true"/> to return the rows without tracking them.</param>
        /// <param name="cancellationToken">A token to cancel the query.</param>
        /// <returns>The page, whether more rows follow and the cursor of the next page.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> is less than 1.</exception>
        public static Task<KeysetPage<TEntity, TKey>> GetPageAfterAsync<TEntity, TKey>(
            this IReadRepository<TEntity> repository,
            Expression<Func<TEntity, TKey>> keySelector,
            TKey? after,
            int pageSize,
            Expression<Func<TEntity, bool>>? filter = null,
            bool descending = false,
            bool asNoTracking = false,
            CancellationToken cancellationToken = default)
            where TEntity : class
            where TKey : class, IComparable<TKey>
        {
            IQueryable<TEntity> query = FilteredKeysetSource(repository, keySelector, pageSize, filter, asNoTracking);

            return ReadKeysetPageAsync(query, keySelector, after != null, after!, pageSize, descending, cancellationToken);
        }

        /// <summary>
        /// Reads one page of the rows selected by <paramref name="specification"/> with keyset (cursor) pagination, with
        /// the same behaviour as the filter overload of <c>GetPageAfterAsync</c>. The conditions, includes and query
        /// options of the specification (<c>AsNoTracking</c>, <c>AsNoTrackingWithIdentityResolution</c>,
        /// <c>AsSplitQuery</c>, <c>IgnoreQueryFilters</c>) apply; the order comes from <paramref name="keySelector"/>,
        /// so a specification with an <c>OrderBy</c>, with <c>Skip</c>/<c>Take</c>, or of type
        /// <see cref="PaginationSpecification{TEntity}"/> is rejected.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TKey">The value type of the key, for example <see cref="int"/>, <see cref="long"/> or <see cref="DateTime"/>.</typeparam>
        /// <param name="repository">The repository whose context runs the query.</param>
        /// <param name="keySelector">The key the rows are ordered and paged by, for example <c>p =&gt; p.Id</c>.</param>
        /// <param name="after">The key of the last row of the previous page, or <see langword="null"/> for the first page.</param>
        /// <param name="pageSize">The maximum number of rows to return; must be greater than 0.</param>
        /// <param name="specification">The specification that selects and shapes the rows.</param>
        /// <param name="descending"><see langword="true"/> to page from the largest key to the smallest.</param>
        /// <param name="cancellationToken">A token to cancel the query.</param>
        /// <returns>The page, whether more rows follow and the cursor of the next page.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="repository"/>, <paramref name="keySelector"/> or <paramref name="specification"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> is less than 1.</exception>
        /// <exception cref="ArgumentException">The specification has an <c>OrderBy</c>, <c>Skip</c>/<c>Take</c> or a <see langword="null"/> condition, or is a <see cref="PaginationSpecification{TEntity}"/>.</exception>
        public static Task<KeysetPage<TEntity, TKey>> GetPageAfterAsync<TEntity, TKey>(
            this IReadRepository<TEntity> repository,
            Expression<Func<TEntity, TKey>> keySelector,
            TKey? after,
            int pageSize,
            SpecificationBase<TEntity> specification,
            bool descending = false,
            CancellationToken cancellationToken = default)
            where TEntity : class
            where TKey : struct, IComparable<TKey>
        {
            IQueryable<TEntity> query = SpecifiedKeysetSource(repository, keySelector, pageSize, specification);

            return ReadKeysetPageAsync(query, keySelector, after.HasValue, after.GetValueOrDefault(), pageSize, descending, cancellationToken);
        }

        /// <summary>
        /// Reads one page of the rows selected by <paramref name="specification"/> with keyset (cursor) pagination by a
        /// reference-type key such as a <see cref="string"/>, with the same behaviour and specification rules as the
        /// value-type specification overload of <c>GetPageAfterAsync</c>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TKey">The reference type of the key, for example <see cref="string"/>.</typeparam>
        /// <param name="repository">The repository whose context runs the query.</param>
        /// <param name="keySelector">The key the rows are ordered and paged by, for example <c>p =&gt; p.Code</c>.</param>
        /// <param name="after">The key of the last row of the previous page, or <see langword="null"/> for the first page.</param>
        /// <param name="pageSize">The maximum number of rows to return; must be greater than 0.</param>
        /// <param name="specification">The specification that selects and shapes the rows.</param>
        /// <param name="descending"><see langword="true"/> to page from the largest key to the smallest.</param>
        /// <param name="cancellationToken">A token to cancel the query.</param>
        /// <returns>The page, whether more rows follow and the cursor of the next page.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="repository"/>, <paramref name="keySelector"/> or <paramref name="specification"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> is less than 1.</exception>
        /// <exception cref="ArgumentException">The specification has an <c>OrderBy</c>, <c>Skip</c>/<c>Take</c> or a <see langword="null"/> condition, or is a <see cref="PaginationSpecification{TEntity}"/>.</exception>
        public static Task<KeysetPage<TEntity, TKey>> GetPageAfterAsync<TEntity, TKey>(
            this IReadRepository<TEntity> repository,
            Expression<Func<TEntity, TKey>> keySelector,
            TKey? after,
            int pageSize,
            SpecificationBase<TEntity> specification,
            bool descending = false,
            CancellationToken cancellationToken = default)
            where TEntity : class
            where TKey : class, IComparable<TKey>
        {
            IQueryable<TEntity> query = SpecifiedKeysetSource(repository, keySelector, pageSize, specification);

            return ReadKeysetPageAsync(query, keySelector, after != null, after!, pageSize, descending, cancellationToken);
        }

        private static void ValidateKeysetArguments<TEntity, TKey>(IReadRepository<TEntity> repository, Expression<Func<TEntity, TKey>> keySelector, int pageSize)
            where TEntity : class
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "The value of pageSize must be greater than 0.");
            }
        }

        private static IQueryable<TEntity> FilteredKeysetSource<TEntity, TKey>(
            IReadRepository<TEntity> repository,
            Expression<Func<TEntity, TKey>> keySelector,
            int pageSize,
            Expression<Func<TEntity, bool>>? filter,
            bool asNoTracking)
            where TEntity : class
        {
            ValidateKeysetArguments(repository, keySelector, pageSize);

            IQueryable<TEntity> query = repository.Table.Set<TEntity>();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }

        private static IQueryable<TEntity> SpecifiedKeysetSource<TEntity, TKey>(
            IReadRepository<TEntity> repository,
            Expression<Func<TEntity, TKey>> keySelector,
            int pageSize,
            SpecificationBase<TEntity> specification)
            where TEntity : class
        {
            ValidateKeysetArguments(repository, keySelector, pageSize);

            if (specification == null)
            {
                throw new ArgumentNullException(nameof(specification));
            }

            if (specification is PaginationSpecification<TEntity>)
            {
                throw new ArgumentException("A pagination specification cannot be combined with keyset pagination.", nameof(specification));
            }

            if (specification is Specification<TEntity> limited && (limited.Skip != null || limited.Take != null))
            {
                throw new ArgumentException("Skip and Take cannot be combined with keyset pagination.", nameof(specification));
            }

            if (specification.OrderBy != null)
            {
                throw new ArgumentException("Keyset pagination orders the rows by its key, so the specification cannot have an OrderBy.", nameof(specification));
            }

            if (specification.Conditions != null && specification.Conditions.Any(condition => condition == null))
            {
                throw new ArgumentException("The specification cannot contain null conditions.", nameof(specification));
            }

            return repository.Table.Set<TEntity>().GetSpecifiedQuery(specification);
        }

        private static async Task<KeysetPage<TEntity, TKey>> ReadKeysetPageAsync<TEntity, TKey>(
            IQueryable<TEntity> query,
            Expression<Func<TEntity, TKey>> keySelector,
            bool hasCursor,
            TKey after,
            int pageSize,
            bool descending,
            CancellationToken cancellationToken)
            where TEntity : class
        {
            if (hasCursor)
            {
                query = query.Where(KeysetCondition(keySelector, after, descending));
            }

            query = descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);

            int take = pageSize == int.MaxValue ? pageSize : pageSize + 1;

            List<TEntity> rows = await query.Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);

            bool hasNextPage = rows.Count > pageSize;

            if (hasNextPage)
            {
                rows.RemoveRange(pageSize, rows.Count - pageSize);
            }

            TKey? nextCursor = rows.Count == 0 ? default : keySelector.Compile()(rows[rows.Count - 1]);

            return new KeysetPage<TEntity, TKey>(rows, pageSize, hasNextPage, nextCursor);
        }

        private static Expression<Func<TEntity, bool>> KeysetCondition<TEntity, TKey>(Expression<Func<TEntity, TKey>> keySelector, TKey after, bool descending)
        {
            Expression<Func<TKey>> cursor = () => after;
            Expression key = keySelector.Body;
            Expression value = cursor.Body;
            Expression comparison;

            if (IsNumeric(typeof(TKey)) || typeof(TKey).GetMethod("op_GreaterThan", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(TKey), typeof(TKey) }, null) != null)
            {
                comparison = descending ? Expression.LessThan(key, value) : Expression.GreaterThan(key, value);
            }
            else
            {
                MethodInfo compareTo = typeof(TKey).GetMethod(nameof(IComparable<TKey>.CompareTo), new[] { typeof(TKey) })
                    ?? typeof(IComparable<TKey>).GetMethod(nameof(IComparable<TKey>.CompareTo))!;
                Expression compared = Expression.Call(key, compareTo, value);
                Expression zero = Expression.Constant(0);
                comparison = descending ? Expression.LessThan(compared, zero) : Expression.GreaterThan(compared, zero);
            }

            return Expression.Lambda<Func<TEntity, bool>>(comparison, keySelector.Parameters);
        }

        private static bool IsNumeric(Type type)
        {
            if (type.IsEnum)
            {
                return false;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;
                default:
                    return false;
            }
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
