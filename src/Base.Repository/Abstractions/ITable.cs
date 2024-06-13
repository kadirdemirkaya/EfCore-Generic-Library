using Microsoft.EntityFrameworkCore;

namespace Base.Repository.Abstractions
{
    public interface ITable
    {
        public DbContext Table { get; }
    }
}
