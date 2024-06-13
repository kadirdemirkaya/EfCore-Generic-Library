using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EfCore.Repository
{
    public static class SpecificationEvaluator
    {
        public static IQueryable<TEntity> GetSpecifiedQuery<TEntity>(this IQueryable<TEntity> inputQuery, Specification<TEntity> specification)
            where TEntity : class
        {
            IQueryable<TEntity> query = GetSpecifiedQuery(inputQuery, (SpecificationBase<TEntity>)specification);

            //Add_AsNoTracking(query, specification);

            Add_Skip(query, specification);

            Add_Take(query, specification);

            return query;
        }

        public static IQueryable<TEntity> GetSpecifiedQuery<TEntity>(this IQueryable<TEntity> inputQuery, PaginationSpecification<TEntity> specification)
            where TEntity : class
        {
            if (inputQuery == null)
            {
                throw new ArgumentNullException(nameof(inputQuery));
            }

            if (specification == null)
            {
                throw new ArgumentNullException(nameof(specification));
            }

            if (specification.PageIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(specification), "The value of specification.PageIndex must be greater than 0.");
            }

            if (specification.PageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(specification), "The value of specification.PageSize must be greater than 0.");
            }

            IQueryable<TEntity> query = GetSpecifiedQuery(inputQuery, (SpecificationBase<TEntity>)specification);

            if (specification.AsNoTracking)
            {
                query = query.AsNoTracking();
            }

            int skip = (specification.PageIndex - 1) * specification.PageSize;

            query = query.Skip(skip).Take(specification.PageSize);

            return query;
        }

        public static IQueryable<TEntity> GetSpecifiedQuery<TEntity>(this IQueryable<TEntity> inputQuery, SpecificationBase<TEntity> specification)
            where TEntity : class
        {
            if (inputQuery == null)
            {
                throw new ArgumentNullException(nameof(inputQuery));
            }

            if (specification == null)
            {
                throw new ArgumentNullException(nameof(specification));
            }

            IQueryable<TEntity> query = inputQuery;

            if (specification.Conditions != null && specification.Conditions.Count != 0)
            {
                foreach (Expression<Func<TEntity, bool>> specificationCondition in specification.Conditions)
                {
                    query = query.Where(specificationCondition);
                }
            }

            if (specification.AsNoTracking)
            {
                query = query.AsNoTracking();
            }

            if (specification.Includes != null)
            {
                query = specification.Includes(query);
            }

            if (specification.OrderBy != null)
            {
                query = specification.OrderBy(query);
            }

            return query;
        }

        private static void Add_AsNoTracking<TEntity>(IQueryable<TEntity> query, Specification<TEntity> specification)
             where TEntity : class
        {
            if (specification.AsNoTracking)
            {
                query = query.AsNoTracking();
            }
        }

        private static void Add_Skip<TEntity>(IQueryable<TEntity> query, Specification<TEntity> specification)
            where TEntity : class
        {
            if (specification.Skip != null)
            {
                if (specification.Skip < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(specification), $"The value of {nameof(specification.Skip)} in {nameof(specification)} can not be negative.");
                }

                query = query.Skip((int)specification.Skip);
            }
        }

        private static void Add_Take<TEntity>(IQueryable<TEntity> query, Specification<TEntity> specification)
              where TEntity : class
        {
            if (specification.Take != null)
            {
                if (specification.Take < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(specification), $"The value of {nameof(specification.Take)} in {nameof(specification)} can not be negative.");
                }

                query = query.Take((int)specification.Take);
            }
        }
    }
}
