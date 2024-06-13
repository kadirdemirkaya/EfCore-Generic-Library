using Base.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Repository.Concretes
{
    public class Table : ITable
    {
        public DbContext _dbContext { get; set; }

        public Table(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        DbContext ITable.Table => _dbContext;
    }
}
