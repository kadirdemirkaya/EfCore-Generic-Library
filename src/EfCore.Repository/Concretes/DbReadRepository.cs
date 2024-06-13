using Base.Repository.Options;
using EfCore.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EfCore.Repository.Concretes
{
    public class DbReadRepository<TEntity> : ReadRepository<TEntity>, IDbReadRepository<TEntity>
        where TEntity : class, new()
    {
        public DbReadRepository(DbContext dbContext) : base(dbContext)
        {
        }

        public DbReadRepository(DbContext dbContext, IServiceProvider serviceProvider) : base(dbContext, serviceProvider)
        {
        }

        public DbReadRepository(DatabaseOptions databaseOptions, IServiceProvider serviceProvider) : base(databaseOptions, serviceProvider)
        {
        }
    }
}
