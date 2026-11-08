using Base.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Repository.Concretes
{
    /// <summary>
    /// Gives access to the <see cref="DbContext"/> a unit of work operates on, through <see cref="ITable.Table"/>.
    /// </summary>
    public class Table : ITable
    {
        private readonly DbContext _dbContext;

        /// <summary>
        /// Wraps <paramref name="dbContext"/>.
        /// </summary>
        /// <param name="dbContext">The context returned by <see cref="ITable.Table"/>.</param>
        public Table(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        DbContext ITable.Table => _dbContext;
    }
}
