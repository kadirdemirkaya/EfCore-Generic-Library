using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace EfCore.Repository
{
    /// <summary>
    /// Fluent configuration of <see cref="Specification{TEntity}"/> and <see cref="PaginationSpecification{TEntity}"/>.
    /// Every method changes the specification it is called on and returns that same instance, so calls can be chained;
    /// a specification built this way is identical to one built by setting its properties and both styles can be mixed.
    /// Conditions added with <c>Where</c> are combined with AND; combine conditions with OR or NOT through
    /// <see cref="PredicateExtensions"/> and pass the result to <c>Where</c>.
    /// </summary>
    public static class SpecificationBuilderExtensions
    {
        /// <summary>Adds <paramref name="condition"/> to <see cref="SpecificationBase{TEntity}.Conditions"/>; all conditions must match.</summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="specification">The specification to change.</param>
        /// <param name="condition">The condition the rows must match.</param>
        /// <returns><paramref name="specification"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="specification"/> or <paramref name="condition"/> is <see langword="null"/>.</exception>
        public static Specification<TEntity> Where<TEntity>(this Specification<TEntity> specification, Expression<Func<TEntity, bool>> condition)
            where TEntity : class
            => AddCondition(specification, condition);

        /// <inheritdoc cref="Where{TEntity}(Specification{TEntity}, Expression{Func{TEntity, bool}})"/>
        public static PaginationSpecification<TEntity> Where<TEntity>(this PaginationSpecification<TEntity> specification, Expression<Func<TEntity, bool>> condition)
            where TEntity : class
            => AddCondition(specification, condition);

        /// <summary>
        /// Loads the related data chosen by <paramref name="navigation"/> together with the rows. Includes already set on
        /// <see cref="SpecificationBase{TEntity}.Includes"/> are kept and this one is added after them.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TProperty">The type of the navigation (a related entity or a collection of them).</typeparam>
        /// <param name="specification">The specification to change.</param>
        /// <param name="navigation">The navigation to load, for example <c>p =&gt; p.Orders</c>.</param>
        /// <returns><paramref name="specification"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="specification"/> or <paramref name="navigation"/> is <see langword="null"/>.</exception>
        public static Specification<TEntity> Include<TEntity, TProperty>(this Specification<TEntity> specification, Expression<Func<TEntity, TProperty>> navigation)
            where TEntity : class
            where TProperty : class
            => AddInclude(specification, navigation);

        /// <inheritdoc cref="Include{TEntity, TProperty}(Specification{TEntity}, Expression{Func{TEntity, TProperty}})"/>
        public static PaginationSpecification<TEntity> Include<TEntity, TProperty>(this PaginationSpecification<TEntity> specification, Expression<Func<TEntity, TProperty>> navigation)
            where TEntity : class
            where TProperty : class
            => AddInclude(specification, navigation);

        /// <summary>
        /// Adds an include chain such as <c>q =&gt; q.Include(p =&gt; p.Orders).ThenInclude(o =&gt; o.Lines)</c>. Includes
        /// already set on <see cref="SpecificationBase{TEntity}.Includes"/> are kept and this chain is applied after them.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="specification">The specification to change.</param>
        /// <param name="include">The include chain to apply to the query.</param>
        /// <returns><paramref name="specification"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="specification"/> or <paramref name="include"/> is <see langword="null"/>.</exception>
        public static Specification<TEntity> Include<TEntity>(this Specification<TEntity> specification, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include)
            where TEntity : class
            => AddInclude(specification, include);

        /// <inheritdoc cref="Include{TEntity}(Specification{TEntity}, Func{IQueryable{TEntity}, IIncludableQueryable{TEntity, object}})"/>
        public static PaginationSpecification<TEntity> Include<TEntity>(this PaginationSpecification<TEntity> specification, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include)
            where TEntity : class
            => AddInclude(specification, include);

        /// <summary>
        /// Orders the rows by <paramref name="key"/> in ascending order, replacing any order already set on
        /// <see cref="SpecificationBase{TEntity}.OrderBy"/>. Add further keys with <c>ThenBy</c> / <c>ThenByDescending</c>.
        /// (The name differs from <c>OrderBy</c> because the specification already has a property of that name.)
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="specification">The specification to change.</param>
        /// <param name="key">The key to order by, for example <c>p =&gt; p.Name</c>.</param>
        /// <returns><paramref name="specification"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="specification"/> or <paramref name="key"/> is <see langword="null"/>.</exception>
        public static Specification<TEntity> OrderByAscending<TEntity, TKey>(this Specification<TEntity> specification, Expression<Func<TEntity, TKey>> key)
            where TEntity : class
            => SetOrder(specification, key, false);

        /// <inheritdoc cref="OrderByAscending{TEntity, TKey}(Specification{TEntity}, Expression{Func{TEntity, TKey}})"/>
        public static PaginationSpecification<TEntity> OrderByAscending<TEntity, TKey>(this PaginationSpecification<TEntity> specification, Expression<Func<TEntity, TKey>> key)
            where TEntity : class
            => SetOrder(specification, key, false);

        /// <summary>
        /// Orders the rows by <paramref name="key"/> in descending order, replacing any order already set on
        /// <see cref="SpecificationBase{TEntity}.OrderBy"/>. Add further keys with <c>ThenBy</c> / <c>ThenByDescending</c>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="specification">The specification to change.</param>
        /// <param name="key">The key to order by, for example <c>p =&gt; p.CreatedAt</c>.</param>
        /// <returns><paramref name="specification"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="specification"/> or <paramref name="key"/> is <see langword="null"/>.</exception>
        public static Specification<TEntity> OrderByDescending<TEntity, TKey>(this Specification<TEntity> specification, Expression<Func<TEntity, TKey>> key)
            where TEntity : class
            => SetOrder(specification, key, true);

        /// <inheritdoc cref="OrderByDescending{TEntity, TKey}(Specification{TEntity}, Expression{Func{TEntity, TKey}})"/>
        public static PaginationSpecification<TEntity> OrderByDescending<TEntity, TKey>(this PaginationSpecification<TEntity> specification, Expression<Func<TEntity, TKey>> key)
            where TEntity : class
            => SetOrder(specification, key, true);

        /// <summary>Adds <paramref name="key"/> in ascending order as the next sort key after the order already set.</summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="specification">The specification to change.</param>
        /// <param name="key">The next key to order by.</param>
        /// <returns><paramref name="specification"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="specification"/> or <paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The specification has no order yet (<see cref="SpecificationBase{TEntity}.OrderBy"/> is <see langword="null"/>).</exception>
        public static Specification<TEntity> ThenBy<TEntity, TKey>(this Specification<TEntity> specification, Expression<Func<TEntity, TKey>> key)
            where TEntity : class
            => AddOrder(specification, key, false);

        /// <inheritdoc cref="ThenBy{TEntity, TKey}(Specification{TEntity}, Expression{Func{TEntity, TKey}})"/>
        public static PaginationSpecification<TEntity> ThenBy<TEntity, TKey>(this PaginationSpecification<TEntity> specification, Expression<Func<TEntity, TKey>> key)
            where TEntity : class
            => AddOrder(specification, key, false);

        /// <summary>Adds <paramref name="key"/> in descending order as the next sort key after the order already set.</summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="specification">The specification to change.</param>
        /// <param name="key">The next key to order by.</param>
        /// <returns><paramref name="specification"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="specification"/> or <paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The specification has no order yet (<see cref="SpecificationBase{TEntity}.OrderBy"/> is <see langword="null"/>).</exception>
        public static Specification<TEntity> ThenByDescending<TEntity, TKey>(this Specification<TEntity> specification, Expression<Func<TEntity, TKey>> key)
            where TEntity : class
            => AddOrder(specification, key, true);

        /// <inheritdoc cref="ThenByDescending{TEntity, TKey}(Specification{TEntity}, Expression{Func{TEntity, TKey}})"/>
        public static PaginationSpecification<TEntity> ThenByDescending<TEntity, TKey>(this PaginationSpecification<TEntity> specification, Expression<Func<TEntity, TKey>> key)
            where TEntity : class
            => AddOrder(specification, key, true);

        /// <summary>Sets <see cref="SpecificationBase{TEntity}.AsNoTracking"/>: the results are not tracked by the context.</summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="specification">The specification to change.</param>
        /// <returns><paramref name="specification"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="specification"/> is <see langword="null"/>.</exception>
        public static Specification<TEntity> AsNoTracking<TEntity>(this Specification<TEntity> specification)
            where TEntity : class
            => Configure(specification, s => s.AsNoTracking = true);

        /// <inheritdoc cref="AsNoTracking{TEntity}(Specification{TEntity})"/>
        public static PaginationSpecification<TEntity> AsNoTracking<TEntity>(this PaginationSpecification<TEntity> specification)
            where TEntity : class
            => Configure(specification, s => s.AsNoTracking = true);

        /// <summary>
        /// Sets <see cref="SpecificationBase{TEntity}.AsNoTrackingWithIdentityResolution"/>: the results are not tracked,
        /// but a row that appears several times is materialized as one shared instance.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="specification">The specification to change.</param>
        /// <returns><paramref name="specification"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="specification"/> is <see langword="null"/>.</exception>
        public static Specification<TEntity> AsNoTrackingWithIdentityResolution<TEntity>(this Specification<TEntity> specification)
            where TEntity : class
            => Configure(specification, s => s.AsNoTrackingWithIdentityResolution = true);

        /// <inheritdoc cref="AsNoTrackingWithIdentityResolution{TEntity}(Specification{TEntity})"/>
        public static PaginationSpecification<TEntity> AsNoTrackingWithIdentityResolution<TEntity>(this PaginationSpecification<TEntity> specification)
            where TEntity : class
            => Configure(specification, s => s.AsNoTrackingWithIdentityResolution = true);

        /// <summary>
        /// Sets <see cref="SpecificationBase{TEntity}.AsSplitQuery"/>: included collections are loaded with separate SQL
        /// commands instead of one joined command.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="specification">The specification to change.</param>
        /// <returns><paramref name="specification"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="specification"/> is <see langword="null"/>.</exception>
        public static Specification<TEntity> AsSplitQuery<TEntity>(this Specification<TEntity> specification)
            where TEntity : class
            => Configure(specification, s => s.AsSplitQuery = true);

        /// <inheritdoc cref="AsSplitQuery{TEntity}(Specification{TEntity})"/>
        public static PaginationSpecification<TEntity> AsSplitQuery<TEntity>(this PaginationSpecification<TEntity> specification)
            where TEntity : class
            => Configure(specification, s => s.AsSplitQuery = true);

        /// <summary>
        /// Sets <see cref="SpecificationBase{TEntity}.IgnoreQueryFilters"/>: the global query filters of the model are not applied.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="specification">The specification to change.</param>
        /// <returns><paramref name="specification"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="specification"/> is <see langword="null"/>.</exception>
        public static Specification<TEntity> IgnoreQueryFilters<TEntity>(this Specification<TEntity> specification)
            where TEntity : class
            => Configure(specification, s => s.IgnoreQueryFilters = true);

        /// <inheritdoc cref="IgnoreQueryFilters{TEntity}(Specification{TEntity})"/>
        public static PaginationSpecification<TEntity> IgnoreQueryFilters<TEntity>(this PaginationSpecification<TEntity> specification)
            where TEntity : class
            => Configure(specification, s => s.IgnoreQueryFilters = true);

        /// <summary>Sets <see cref="Specification{TEntity}.Skip"/>: the number of rows to skip.</summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="specification">The specification to change.</param>
        /// <param name="count">The number of rows to skip; cannot be negative.</param>
        /// <returns><paramref name="specification"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="specification"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        public static Specification<TEntity> Skip<TEntity>(this Specification<TEntity> specification, int count)
            where TEntity : class
        {
            EnsureNotNull(specification);
            EnsureNotNegative(count, nameof(count));

            return Configure(specification, s => s.Skip = count);
        }

        /// <summary>Sets <see cref="Specification{TEntity}.Take"/>: the maximum number of rows to return.</summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="specification">The specification to change.</param>
        /// <param name="count">The maximum number of rows; cannot be negative.</param>
        /// <returns><paramref name="specification"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="specification"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        public static Specification<TEntity> Take<TEntity>(this Specification<TEntity> specification, int count)
            where TEntity : class
        {
            EnsureNotNull(specification);
            EnsureNotNegative(count, nameof(count));

            return Configure(specification, s => s.Take = count);
        }

        /// <summary>
        /// Sets <see cref="PaginationSpecification{TEntity}.PageIndex"/> and <see cref="PaginationSpecification{TEntity}.PageSize"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="specification">The specification to change.</param>
        /// <param name="pageIndex">The 1-based page number; must be greater than 0.</param>
        /// <param name="pageSize">The number of rows per page; must be greater than 0.</param>
        /// <returns><paramref name="specification"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="specification"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageIndex"/> or <paramref name="pageSize"/> is less than 1.</exception>
        public static PaginationSpecification<TEntity> Page<TEntity>(this PaginationSpecification<TEntity> specification, int pageIndex, int pageSize)
            where TEntity : class
        {
            EnsureNotNull(specification);

            if (pageIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageIndex), "The value of pageIndex must be greater than 0.");
            }

            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "The value of pageSize must be greater than 0.");
            }

            return Configure(specification, s =>
            {
                s.PageIndex = pageIndex;
                s.PageSize = pageSize;
            });
        }

        private static TSpecification Configure<TSpecification>(TSpecification specification, Action<TSpecification> configure)
            where TSpecification : class
        {
            if (specification == null)
            {
                throw new ArgumentNullException(nameof(specification));
            }

            configure(specification);

            return specification;
        }

        private static TSpecification AddCondition<TSpecification, TEntity>(TSpecification specification, Expression<Func<TEntity, bool>> condition)
            where TSpecification : SpecificationBase<TEntity>
            where TEntity : class
        {
            if (specification == null)
            {
                throw new ArgumentNullException(nameof(specification));
            }

            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            if (specification.Conditions == null)
            {
                specification.Conditions = new List<Expression<Func<TEntity, bool>>>();
            }

            specification.Conditions.Add(condition);

            return specification;
        }

        private static TSpecification AddInclude<TSpecification, TEntity, TProperty>(TSpecification specification, Expression<Func<TEntity, TProperty>> navigation)
            where TSpecification : SpecificationBase<TEntity>
            where TEntity : class
            where TProperty : class
        {
            if (navigation == null)
            {
                throw new ArgumentNullException(nameof(navigation));
            }

            return AddInclude<TSpecification, TEntity>(specification, query => query.Include(navigation));
        }

        private static TSpecification AddInclude<TSpecification, TEntity>(TSpecification specification, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include)
            where TSpecification : SpecificationBase<TEntity>
            where TEntity : class
        {
            if (specification == null)
            {
                throw new ArgumentNullException(nameof(specification));
            }

            if (include == null)
            {
                throw new ArgumentNullException(nameof(include));
            }

            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? previous = specification.Includes;

            specification.Includes = previous == null ? include : query => include(previous(query));

            return specification;
        }

        private static TSpecification SetOrder<TSpecification, TEntity, TKey>(TSpecification specification, Expression<Func<TEntity, TKey>> key, bool descending)
            where TSpecification : SpecificationBase<TEntity>
            where TEntity : class
        {
            if (specification == null)
            {
                throw new ArgumentNullException(nameof(specification));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            specification.OrderBy = descending
                ? query => query.OrderByDescending(key)
                : query => query.OrderBy(key);

            return specification;
        }

        private static TSpecification AddOrder<TSpecification, TEntity, TKey>(TSpecification specification, Expression<Func<TEntity, TKey>> key, bool descending)
            where TSpecification : SpecificationBase<TEntity>
            where TEntity : class
        {
            if (specification == null)
            {
                throw new ArgumentNullException(nameof(specification));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> previous = specification.OrderBy
                ?? throw new InvalidOperationException("ThenBy and ThenByDescending need an order set first with OrderByAscending, OrderByDescending or the OrderBy property.");

            specification.OrderBy = descending
                ? query => previous(query).ThenByDescending(key)
                : query => previous(query).ThenBy(key);

            return specification;
        }

        private static void EnsureNotNull(object specification)
        {
            if (specification == null)
            {
                throw new ArgumentNullException(nameof(specification));
            }
        }

        private static void EnsureNotNegative(int count, string parameterName)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "The value cannot be negative.");
            }
        }
    }
}
