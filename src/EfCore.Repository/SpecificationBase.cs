using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace EfCore.Repository
{
    public class SpecificationBase<TEntity>
    where TEntity : class
    {
        public bool AsNoTracking { get; set; } = false;

        public List<Expression<Func<TEntity, bool>>> Conditions { get; set; } = new List<Expression<Func<TEntity, bool>>>();

        public Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> Includes { get; set; }

        public Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> OrderBy { get; set; }
    }
}
