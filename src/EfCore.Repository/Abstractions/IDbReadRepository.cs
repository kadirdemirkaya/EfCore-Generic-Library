using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EfCore.Repository.Abstractions
{
    public interface IDbReadRepository<TEntity> : IReadRepository<TEntity>
        where TEntity : class, new()
    {
    }
}
