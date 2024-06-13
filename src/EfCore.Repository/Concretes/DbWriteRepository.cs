using Base.Repository.Options;
using EfCore.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Repository.Concretes
{
    public class DbWriteRepository<TEntity> : WriteRepository<TEntity>, IDbWriteRepository<TEntity>
        where TEntity : class, new()
    {
        public DbWriteRepository(DbContext dbContext) : base(dbContext)
        {
        }

        public DbWriteRepository(DbContext dbContext, IServiceProvider serviceProvider) : base(dbContext, serviceProvider)
        {
        }

        public DbWriteRepository(DatabaseOptions databaseOptions, IServiceProvider serviceProvider) : base(databaseOptions, serviceProvider)
        {
        }
    }
}
