using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EfCore.Repository
{
    public static class QueryableExtensions
    {
        public static async Task<PaginatedList<TEntity>> ToPaginatedListAsync<TEntity>(
            this IQueryable<TEntity> entities,
            int pageIndex,
            int pageSize,
            CancellationToken cancellationToken = default)
            where TEntity : class
        {
            ValidatePage(entities, pageIndex, pageSize);

            long count = await entities.LongCountAsync(cancellationToken);

            int skip = (pageIndex - 1) * pageSize;

            List<TEntity> items = await entities.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

            return new PaginatedList<TEntity>(items, count, pageIndex, pageSize);
        }

        internal static PaginatedList<TEntity> ToPaginatedList<TEntity>(this IQueryable<TEntity> entities, int pageIndex, int pageSize)
            where TEntity : class
        {
            ValidatePage(entities, pageIndex, pageSize);

            long count = entities.LongCount();

            List<TEntity> items = entities.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

            return new PaginatedList<TEntity>(items, count, pageIndex, pageSize);
        }

        private static void ValidatePage<TEntity>(IQueryable<TEntity> entities, int pageIndex, int pageSize)
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            if (pageIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageIndex), "The value of pageIndex must be greater than 0.");
            }

            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "The value of pageSize must be greater than 0.");
            }
        }

        public static async Task<PaginatedList<TEntity>> ToPaginatedListAsync<TEntity>(
            this IQueryable<TEntity> entities,
            PaginationSpecification<TEntity> specification,
            CancellationToken cancellationToken = default)
            where TEntity : class
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            if (specification == null)
            {
                throw new ArgumentNullException(nameof(specification));
            }

            if (specification.PageIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(specification), $"The value of {nameof(specification.PageIndex)} must be greater than 0.");
            }

            if (specification.PageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(specification), $"The value of {nameof(specification.PageSize)} must be greater than 0.");
            }

            IQueryable<TEntity> countSource = entities;

            if (specification.Conditions != null && specification.Conditions.Count != 0)
            {
                foreach (Expression<Func<TEntity, bool>> condition in specification.Conditions)
                {
                    countSource = countSource.Where(condition);
                }
            }

            long count = await countSource.LongCountAsync(cancellationToken);

            entities = entities.GetSpecifiedQuery(specification);
            List<TEntity> items = await entities.ToListAsync(cancellationToken);

            return new PaginatedList<TEntity>(items, count, specification.PageIndex, specification.PageSize);
        }
    }
}
