using Base.Repository.Abstractions;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace EfCore.Repository.Abstractions
{
    public interface IReadRepository<TEntity> : IBaseReadRepository<TEntity>
        where TEntity : class
    {
        #region GetList
        Task<List<TEntity>> GetListAsync(bool asNoTracking, CancellationToken cancellationToken = default);

        Task<List<TEntity>> GetListAsync(
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includes,
        CancellationToken cancellationToken = default);

        Task<List<TEntity>> GetListAsync(
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includes,
        bool asNoTracking,
        CancellationToken cancellationToken = default);

        Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

        Task<List<TEntity>> GetListAsync(
            Expression<Func<TEntity, bool>> filter,
            bool asNoTracking,
            CancellationToken cancellationToken = default);

        Task<List<TEntity>> GetListAsync(
            Expression<Func<TEntity, bool>> filter,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includes,
            bool asNoTracking = false,
            CancellationToken cancellationToken = default);

        Task<List<TEntity>> GetListAsync(Specification<TEntity> specification, CancellationToken cancellationToken = default);

        Task<List<TEntity>> GetListAsync(
            Specification<TEntity> specification,
            bool asNoTracking,
            CancellationToken cancellationToken = default);

        Task<List<MapType>> GetListAsync<MapType>(
            Expression<Func<TEntity, MapType>> mapFilter,
            CancellationToken cancellationToken = default);

        Task<List<MapType>> GetListAsync<MapType>(
            Expression<Func<TEntity, bool>> filter,
            Expression<Func<TEntity, MapType>> mapFilter,
            CancellationToken cancellationToken = default);

        Task<List<MapType>> GetListAsync<MapType>(
            Specification<TEntity> specification,
            Expression<Func<TEntity, MapType>> mapFilter,
            CancellationToken cancellationToken = default);

        Task<PaginatedList<TEntity>> GetListAsync(
            PaginationSpecification<TEntity> specification,
            CancellationToken cancellationToken = default);

        Task<PaginatedList<MapType>> GetListAsync<MapType>(
            PaginationSpecification<TEntity> specification,
            Expression<Func<TEntity, MapType>> mapFilter,
            CancellationToken cancellationToken = default)
            where MapType : class;
        #endregion





        #region GetById
        Task<TEntity> GetByIdAsync(object id, bool asNoTracking, CancellationToken cancellationToken = default);

        Task<TEntity> GetByIdAsync(
           object id,
           Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includes,
           CancellationToken cancellationToken = default);

        Task<TEntity> GetByIdAsync(
         object id,
         Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includes,
         bool asNoTracking,
         CancellationToken cancellationToken = default);


        Task<MapType> GetByIdAsync<MapType>(
           object id,
           Expression<Func<TEntity, MapType>> mapFilter,
            bool asNoTracking,
           CancellationToken cancellationToken = default);
        #endregion





        #region Get
        Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

        Task<TEntity> GetAsync(
            Expression<Func<TEntity, bool>> filter,
            bool asNoTracking,
            CancellationToken cancellationToken = default);

        Task<TEntity> GetAsync(
            Expression<Func<TEntity, bool>> filter,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includes,
            CancellationToken cancellationToken = default);

        Task<TEntity> GetAsync(
           Expression<Func<TEntity, bool>> filter,
           Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includes,
           bool asNoTracking,
           CancellationToken cancellationToken = default);

        Task<TEntity> GetAsync(Specification<TEntity> specification, CancellationToken cancellationToken = default);

        Task<TEntity> GetAsync(
          Specification<TEntity> specification,
          bool asNoTracking,
          CancellationToken cancellationToken = default);

        Task<MapType> GetAsync<MapType>(
           Expression<Func<TEntity, bool>> filter,
           Expression<Func<TEntity, MapType>> mapFilter,
           CancellationToken cancellationToken = default);

        Task<MapType> GetAsync<MapType>(
           Specification<TEntity> specification,
           Expression<Func<TEntity, MapType>> mapFilter,
           CancellationToken cancellationToken = default);
        #endregion




        #region Count
        Task<int> CountAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default);

        Task<int> CountAsync(IEnumerable<Expression<Func<TEntity, bool>>> filters, CancellationToken cancellationToken = default);
        #endregion



        #region Any
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> filter, bool asNoTracking = false, CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(IEnumerable<Expression<Func<TEntity, bool>>> filters, bool asNoTracking = false,CancellationToken cancellationToken = default);

        bool Any(out TEntity entity);

        bool Any(out TEntity entity, Expression<Func<TEntity, bool>> filter, bool asNoTracking = false);

        bool Any(out List<TEntity> entity, Expression<Func<TEntity, bool>> filter, bool asNoTracking = false);

        bool Any(out List<TEntity> entities,
           Expression<Func<TEntity, bool>> filter,
           Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includes,
           bool asNoTracking = false);

        bool Any<MapType>(out List<MapType> projectedTypes,
           Expression<Func<TEntity, bool>> filter,
           Expression<Func<TEntity, MapType>> mapFilter);

        bool Any<MapType>(out PaginatedList<MapType> paginatedList,
           PaginationSpecification<TEntity> specification,
           Expression<Func<TEntity, MapType>> mapFilter,
           CancellationToken cancellationToken = default)
           where MapType : class;

        bool Any<MapType>(out List<MapType> projectedTypes,
            Specification<TEntity> specification,
            Expression<Func<TEntity, MapType>> mapFilter);
        #endregion
    }
}
