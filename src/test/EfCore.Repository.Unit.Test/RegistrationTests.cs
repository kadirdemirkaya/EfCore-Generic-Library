using Base.Repository.Abstractions;
using Base.Repository.Options;
using EfCore.Repository.Abstractions;
using EfCore.Repository.Extensions;
using EfCore.Repository.Factory;
using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class RegistrationTests
    {
        private SqliteTestDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();
            _database.Seed(SqliteTestDatabase.SamplePersons());
        }

        [TearDown]
        public void TearDown() => _database.Dispose();

        [Test]
        public async Task EntityRegistration_ResolvesUnitOfWorkAndRepositories()
        {
            ServiceCollection services = new();
            services.EfCoreRepositoryServiceRegistration<Person, TestDbContext>(ServiceLifetime.Scoped);
            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();

            IUnitOfWork<Person> unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<Person>>();

            Assert.Multiple(async () =>
            {
                Assert.That(await unitOfWork.GetReadRepository().CountAsync(), Is.EqualTo(5));
                Assert.That(await unitOfWork.GetDbReadRepository().CountAsync(), Is.EqualTo(5));
                Assert.That(await unitOfWork.GetBaseReadRepository().CountAsync(), Is.EqualTo(5));
                Assert.That(unitOfWork.GetWriteRepository(), Is.Not.Null);
                Assert.That(unitOfWork.GetDbWriteRepository(), Is.Not.Null);
                Assert.That(unitOfWork.GetBaseWriteRepository(), Is.Not.Null);
            });
        }

        [Test]
        public async Task EntityRegistration_UnitOfWorkSavesRepositoryChanges()
        {
            ServiceCollection services = new();
            services.EfCoreRepositoryServiceRegistration<Person, TestDbContext>(ServiceLifetime.Scoped);
            using ServiceProvider provider = services.BuildServiceProvider();
            IUnitOfWork<Person> unitOfWork = provider.CreateScope().ServiceProvider.GetRequiredService<IUnitOfWork<Person>>();

            await unitOfWork.GetDbWriteRepository().AddAsync(new Person { Name = "Margaret", Age = 88 });
            bool saved = await unitOfWork.SaveChangesAsync();

            using TestDbContext context = _database.CreateContext();
            Assert.Multiple(() =>
            {
                Assert.That(saved, Is.True);
                Assert.That(context.Persons.Count(), Is.EqualTo(6));
            });
        }

        [Test]
        public async Task OptionsRegistration_ResolvesRepositories()
        {
            using TestDbContext context = _database.CreateContext();
            ServiceCollection services = new();
            services.EfCoreRepositoryServiceRegistration<Person, TestDbContext>(ServiceLifetime.Scoped, new DatabaseOptions { Connection = context });
            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();

            IUnitOfWork<Person> unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<Person>>();

            Assert.That(await unitOfWork.GetReadRepository().CountAsync(), Is.EqualTo(5));
        }

        [Test]
        public async Task OptionsRegistration_UnitOfWorkSavesChanges()
        {
            using TestDbContext context = _database.CreateContext();
            ServiceCollection services = new();
            services.EfCoreRepositoryServiceRegistration<Person, TestDbContext>(ServiceLifetime.Scoped, new DatabaseOptions { Connection = context });
            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();
            IUnitOfWork<Person> unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<Person>>();

            await unitOfWork.GetWriteRepository().AddAsync(new Person { Name = "Margaret", Age = 88 });
            bool saved = await unitOfWork.SaveChangesAsync();

            Assert.Multiple(() =>
            {
                Assert.That(saved, Is.True);
                Assert.That(unitOfWork.GetTable().Table, Is.SameAs(context));
            });
        }

        [Test]
        public async Task OptionsRegistration_ContextWithoutParameterlessConstructor_ResolvesUnitOfWork()
        {
            using OptionsOnlyDbContext context = new(_database.CreateOptions());
            ServiceCollection services = new();
            services.EfCoreRepositoryServiceRegistration<Person, OptionsOnlyDbContext>(ServiceLifetime.Scoped, new DatabaseOptions { Connection = context });
            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();

            IUnitOfWork<Person> unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<Person>>();

            Assert.That(await unitOfWork.GetReadRepository().CountAsync(), Is.EqualTo(5));
        }

        [Test]
        public async Task RepositoryFactory_WithOptions_UsesOptionsConnection()
        {
            using TestDbContext context = _database.CreateContext();

            IUnitOfWork<Person> unitOfWork = RepositoryFactory<TestDbContext>.CreateUnitOfWork<Person>(new DatabaseOptions { Connection = context });
            context.Persons.Add(new Person { Name = "Margaret" });

            Assert.That(await unitOfWork.SaveChangesAsync(), Is.True);
        }

        [Test]
        public async Task AssemblyRegistration_ResolvesRepositoriesForEveryEntity()
        {
            ServiceCollection services = new();
            services.AddDbContext<TestDbContext>(_database.ConfigureContext);
            services.EfCoreRepositoryServiceRegistration<ITestEntity, TestDbContext>(ServiceLifetime.Scoped, typeof(ITestEntity).Assembly);
            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();

            IUnitOfWork<Person> persons = scope.ServiceProvider.GetRequiredService<IUnitOfWork<Person>>();
            IReadRepository<Basket> baskets = scope.ServiceProvider.GetRequiredService<IReadRepository<Basket>>();

            Assert.Multiple(async () =>
            {
                Assert.That(await persons.GetReadRepository().CountAsync(), Is.EqualTo(5));
                Assert.That(await baskets.CountAsync(), Is.EqualTo(3));
                Assert.That(scope.ServiceProvider.GetRequiredService<IBaseWriteRepository<Person>>(), Is.Not.Null);
            });
        }
    }
}
