using EfCore.Repository.Unit.Test.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Repository.Unit.Test.Data
{
    public sealed class SqliteTestDatabase : IDisposable
    {
        private readonly SqliteConnection _keepAliveConnection;

        public SqliteTestDatabase()
        {
            ConnectionString = $"Data Source=test-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            _keepAliveConnection = new SqliteConnection(ConnectionString);
            _keepAliveConnection.Open();
            TestDbContext.DefaultConnectionString = ConnectionString;

            using TestDbContext context = CreateContext();
            context.Database.EnsureCreated();
        }

        public string ConnectionString { get; }

        public DbContextOptions<TestDbContext> CreateOptions()
            => new DbContextOptionsBuilder<TestDbContext>().UseSqlite(ConnectionString).Options;

        public TestDbContext CreateContext() => new(CreateOptions());

        public TestDbContext CreateContext(CommandCounter counter)
            => new(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(ConnectionString).AddInterceptors(counter).Options);

        public void ConfigureContext(DbContextOptionsBuilder builder)
            => builder.UseSqlite(ConnectionString);

        public void Seed(params Person[] persons)
        {
            using TestDbContext context = CreateContext();
            context.Persons.AddRange(persons);
            context.SaveChanges();
        }

        public static Person[] SamplePersons() => new[]
        {
            new Person { Name = "Ada", Age = 36, Baskets = { new Basket { Description = "books" }, new Basket { Description = "tools" } } },
            new Person { Name = "Alan", Age = 41, Baskets = { new Basket { Description = "games" } } },
            new Person { Name = "Grace", Age = 85 },
            new Person { Name = "Linus", Age = 28 },
            new Person { Name = "Barbara", Age = 52 }
        };

        public void Dispose() => _keepAliveConnection.Dispose();
    }
}
