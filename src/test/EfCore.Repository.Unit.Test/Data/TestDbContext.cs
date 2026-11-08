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

        public DbSet<OrderLine> OrderLines { get; set; }

        public DbSet<VersionedNote> VersionedNotes { get; set; }

        public DbSet<ShadowKeyItem> ShadowKeyItems { get; set; }

        public DbSet<FieldKeyItem> FieldKeyItems { get; set; }

        public TestDbContext()
        {
        }

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderLine>().HasKey(l => new { l.OrderId, l.LineNumber });

            modelBuilder.Entity<ShadowKeyItem>().Property<int>("Id");
            modelBuilder.Entity<ShadowKeyItem>().HasKey("Id");

            modelBuilder.Entity<FieldKeyItem>().Property<int>("_id").HasColumnName("Id");
            modelBuilder.Entity<FieldKeyItem>().HasKey("_id");
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
