using Base.Repository.Abstractions;

namespace EfCore.Repository.Abstractions
{
    public interface IWriteRepository<TEntity> : IBaseWriteRepository<TEntity>
        where TEntity : class
    {
        #region Insert
        Task<object[]> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task<object[]> InsertAsync(List<TEntity> entities, CancellationToken cancellationToken = default);

        Task InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        #endregion


        #region Update
        Task<bool> UpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        #endregion


        #region Delete
        Task<bool> DeleteAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        #endregion


        #region Remove
        void Remove(IEnumerable<TEntity> entities);
        #endregion



        #region Add
        void Add(TEntity entity);
        void Add(IEnumerable<TEntity> entities);
        Task AddAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        #endregion


        #region SaveChanges
        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChangesCountAsync(CancellationToken cancellationToken = default);
        #endregion
    }
}
