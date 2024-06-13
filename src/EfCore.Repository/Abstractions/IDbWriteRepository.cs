namespace EfCore.Repository.Abstractions
{
    public interface IDbWriteRepository<TEntity> : IWriteRepository<TEntity>
        where TEntity : class, new()
    {
    }
}
