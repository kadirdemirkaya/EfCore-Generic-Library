using Microsoft.EntityFrameworkCore;

namespace EfCore.Repository.Unit.Test.Data
{
    public class OptionsOnlyDbContext : TestDbContext
    {
        public OptionsOnlyDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }
    }
}
