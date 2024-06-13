using EfCore.Test.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EfCore.Test.Data
{
    public class AppDbContext : DbContext
    {
        //public DbSet<Person> Person { get; set; }
        //public DbSet<Basket> Basket { get; set; }
        //public DbSet<Product> Product { get; set; }

        //------------------------------------------------------

        public DbSet<Person> Persons { get; set; }
        public DbSet<Basket> Baskets { get; set; }
        public DbSet<Product> Products { get; set; }

        public AppDbContext()
        {

        }

        public AppDbContext(DbContextOptions options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlServer("Server=DESKTOP-KCT444U\\SQLEXPRESS;Database=genericrepocontext;Trusted_Connection=True;TrustServerCertificate=True");

        //protected override void OnConfiguring(DbContextOptionsBuilder options)
        //     => options.UseSqlite("Data Source=MyDatabase.db");

        //protected override void OnConfiguring(DbContextOptionsBuilder options)
        //     => options.UseNpgsql("Server=localhost;port=5432;Database=AppDbContext;User Id=postgresql;Password=123");

        //protected override void OnConfiguring(DbContextOptionsBuilder options)
        //    => options.UseMySql(
        //        "Server=localhost;Port=3306;Database=mydatabase;User Id=user;Password=userpassword;",
        //        new MySqlServerVersion(new Version(8, 0, 21))
        //    );

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

    }
}
