using EfCore.Repository.Unit.Test.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Repository.Unit.Test.Data
{
    public class FilteredDbContext : DbContext
    {
        public DbSet<ArchivableItem> Items { get; set; }

        public FilteredDbContext(DbContextOptions<FilteredDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ArchivableItem>().HasQueryFilter(i => !i.IsArchived);
        }
    }
}
