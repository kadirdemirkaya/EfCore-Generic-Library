using EfCore.Repository.Abstractions;
using EfCore.Repository.Extensions;
using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class RegistrationRegressionTests
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
        public void EntityRegistration_MultipleEntities_ShareOneContext()
        {
            ServiceCollection services = new();
            services.EfCoreRepositoryServiceRegistration<Person, TestDbContext>(ServiceLifetime.Scoped);
            services.EfCoreRepositoryServiceRegistration<Basket, TestDbContext>(ServiceLifetime.Scoped);
            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();

            IUnitOfWork<Person> persons = scope.ServiceProvider.GetRequiredService<IUnitOfWork<Person>>();
            IWriteRepository<Basket> baskets = scope.ServiceProvider.GetRequiredService<IWriteRepository<Basket>>();

            Assert.Multiple(() =>
            {
                Assert.That(baskets.Table, Is.SameAs(persons.GetTable().Table));
                Assert.That(scope.ServiceProvider.GetRequiredService<IReadRepository<Basket>>().Table, Is.SameAs(persons.GetReadRepository().Table));
                Assert.That(scope.ServiceProvider.GetRequiredService<IUnitOfWork<Basket>>().GetTable().Table, Is.SameAs(persons.GetTable().Table));
            });
        }

        [Test]
        public async Task EntityRegistration_MultipleEntities_UnitOfWorkSavesOtherEntityChanges()
        {
            ServiceCollection services = new();
            services.EfCoreRepositoryServiceRegistration<Person, TestDbContext>(ServiceLifetime.Scoped);
            services.EfCoreRepositoryServiceRegistration<Basket, TestDbContext>(ServiceLifetime.Scoped);
            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();

            IUnitOfWork<Person> persons = scope.ServiceProvider.GetRequiredService<IUnitOfWork<Person>>();
            IWriteRepository<Basket> baskets = scope.ServiceProvider.GetRequiredService<IWriteRepository<Basket>>();

            Person ada = await persons.GetReadRepository().GetAsync(p => p.Name == "Ada");
            await baskets.AddAsync(new Basket { Description = "garden", PersonId = ada.Id });
            bool saved = await persons.SaveChangesAsync();

            using TestDbContext context = _database.CreateContext();
            Assert.Multiple(() =>
            {
                Assert.That(saved, Is.True);
                Assert.That(context.Baskets.Count(), Is.EqualTo(4));
            });
        }

        [Test]
        public void EntityRegistration_SeparateServiceCollections_DoNotShareContext()
        {
            ServiceCollection first = new();
            ServiceCollection second = new();
            first.EfCoreRepositoryServiceRegistration<Person, TestDbContext>(ServiceLifetime.Scoped);
            second.EfCoreRepositoryServiceRegistration<Person, TestDbContext>(ServiceLifetime.Scoped);
            using ServiceProvider firstProvider = first.BuildServiceProvider();
            using ServiceProvider secondProvider = second.BuildServiceProvider();

            IReadRepository<Person> firstRepository = firstProvider.CreateScope().ServiceProvider.GetRequiredService<IReadRepository<Person>>();
            IReadRepository<Person> secondRepository = secondProvider.CreateScope().ServiceProvider.GetRequiredService<IReadRepository<Person>>();

            Assert.That(firstRepository.Table, Is.Not.SameAs(secondRepository.Table));
        }

        [Test]
        public void AssemblyRegistration_DoesNotRegisterMarkerType()
        {
            ServiceCollection services = new();

            services.EfCoreRepositoryServiceRegistration<ITestEntity, TestDbContext>(ServiceLifetime.Scoped, typeof(ITestEntity).Assembly);

            Assert.That(services.Any(d => d.ServiceType == typeof(ITestEntity)), Is.False);
        }

        [Test]
        public void AssemblyRegistration_MarkerTypeResolvesWithoutThrowing()
        {
            ServiceCollection services = new();
            services.AddDbContext<TestDbContext>(_database.ConfigureContext);
            services.EfCoreRepositoryServiceRegistration<ITestEntity, TestDbContext>(ServiceLifetime.Scoped, typeof(ITestEntity).Assembly);
            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();

            Assert.Multiple(() =>
            {
                Assert.That(() => scope.ServiceProvider.GetService<ITestEntity>(), Throws.Nothing);
                Assert.That(() => scope.ServiceProvider.GetServices<ITestEntity>().ToList(), Throws.Nothing);
            });
        }

        [Test]
        public void AssemblyRegistration_KeepsExistingMarkerRegistration()
        {
            Person marker = new() { Name = "marker" };
            ServiceCollection services = new();
            services.AddDbContext<TestDbContext>(_database.ConfigureContext);
            services.AddSingleton<ITestEntity>(marker);
            services.EfCoreRepositoryServiceRegistration<ITestEntity, TestDbContext>(ServiceLifetime.Scoped, typeof(ITestEntity).Assembly);
            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();

            Assert.That(scope.ServiceProvider.GetRequiredService<ITestEntity>(), Is.SameAs(marker));
        }

        [Test]
        public void AssemblyRegistration_EntityWithContextConstructor_StillResolvesMarkerType()
        {
            ServiceCollection services = new();
            services.AddDbContext<TestDbContext>(_database.ConfigureContext);
            services.EfCoreRepositoryServiceRegistration<IContextAwareMarker, TestDbContext>(ServiceLifetime.Scoped, typeof(IContextAwareMarker).Assembly);
            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();

            IContextAwareMarker resolved = scope.ServiceProvider.GetRequiredService<IContextAwareMarker>();

            Assert.That(((ContextAwareEntity)resolved).Context, Is.SameAs(scope.ServiceProvider.GetRequiredService<TestDbContext>()));
        }

        public interface IContextAwareMarker
        {
        }

        public class ContextAwareEntity : IContextAwareMarker
        {
            public ContextAwareEntity()
            {
            }

            public ContextAwareEntity(TestDbContext context, IServiceProvider serviceProvider) => Context = context;

            public TestDbContext Context { get; }
        }
    }
}
