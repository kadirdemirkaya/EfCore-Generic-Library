using EfCore.Repository.Unit.Test.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Repository.Unit.Test.Data
{
    public class TestDbContext : DbContext
    {
        public static string DefaultConnectionString { get; set; }

        public DbSet<Person> Persons { get; set; }

        public DbSet<Basket> Baskets { get; set; }

        public DbSet<Tag> Tags { get; set; }

        public TestDbContext()
        {
        }

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(DefaultConnectionString);
            }
        }
    }
}
