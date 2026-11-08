using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace EfCore.Repository
{
    public class SpecificationBase<TEntity>
    where TEntity : class
    {
        public bool AsNoTracking { get; set; } = false;

        public List<Expression<Func<TEntity, bool>>> Conditions { get; set; } = new List<Expression<Func<TEntity, bool>>>();

        public Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? Includes { get; set; }

        public Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? OrderBy { get; set; }

        /// <summary>
        /// When <see langword="true"/>, the query loads included collections with separate SQL commands (one per
        /// collection) instead of one joined command, which avoids repeating the parent columns for every related row.
        /// Requires a relational provider. Use a stable <see cref="OrderBy"/> together with paging, so every command sees
        /// the rows in the same order. Default: <see langword="false"/> (the context's configured query splitting behaviour).
        /// </summary>
        public bool AsSplitQuery { get; set; } = false;

        /// <summary>
        /// When <see langword="true"/>, the global query filters of the entity model (for example soft-delete filters)
        /// are not applied, so rows they hide are returned and, for paginated reads, counted. Default: <see langword="false"/>.
        /// </summary>
        public bool IgnoreQueryFilters { get; set; } = false;

        /// <summary>
        /// When <see langword="true"/>, the results are not tracked by the context, but a row that appears several times
        /// in the result (for example the same parent included by several children) is materialized as one shared
        /// instance. Takes precedence over <see cref="AsNoTracking"/> when both are set. Default: <see langword="false"/>.
        /// </summary>
        public bool AsNoTrackingWithIdentityResolution { get; set; } = false;
    }
}
