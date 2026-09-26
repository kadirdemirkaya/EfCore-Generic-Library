using EfCore.Repository.Abstractions;
using EfCore.Repository.Extensions;
using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class ScopedRegistrationTests
    {
        private SqliteTestDatabase _database;
        private ServiceProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();
            _database.Seed(SqliteTestDatabase.SamplePersons());
            TestDbContext.DefaultConnectionString = "Data Source=:memory:";

            ServiceCollection services = new();
            services.AddDbContext<TestDbContext>(_database.ConfigureContext);
            services.AddEfCoreRepository<Person, TestDbContext>();
            _provider = services.BuildServiceProvider();
        }

        [TearDown]
        public void TearDown()
        {
            _provider.Dispose();
            _database.Dispose();
        }

        [Test]
        public async Task Resolves_UsingContainerConfiguredContext()
        {
            using IServiceScope scope = _provider.CreateScope();

            IUnitOfWork<Person> unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<Person>>();

            Assert.That(await unitOfWork.GetReadRepository().CountAsync(), Is.EqualTo(5));
        }

        [Test]
        public void SameScope_SharesContext()
        {
            using IServiceScope scope = _provider.CreateScope();
            TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

            Assert.Multiple(() =>
            {
                Assert.That(scope.ServiceProvider.GetRequiredService<IReadRepository<Person>>().Table, Is.SameAs(context));
                Assert.That(scope.ServiceProvider.GetRequiredService<IWriteRepository<Person>>().Table, Is.SameAs(context));
                Assert.That(scope.ServiceProvider.GetRequiredService<IDbReadRepository<Person>>().Table, Is.SameAs(context));
                Assert.That(scope.ServiceProvider.GetRequiredService<IDbWriteRepository<Person>>().Table, Is.SameAs(context));
                Assert.That(scope.ServiceProvider.GetRequiredService<Base.Repository.Abstractions.IBaseReadRepository<Person>>().Table, Is.SameAs(context));
                Assert.That(scope.ServiceProvider.GetRequiredService<Base.Repository.Abstractions.IBaseWriteRepository<Person>>().Table, Is.SameAs(context));
                Assert.That(scope.ServiceProvider.GetRequiredService<IUnitOfWork<Person>>().GetTable().Table, Is.SameAs(context));
            });
        }

        [Test]
        public void HiddenTableMembers_ReturnSameContextAsBase()
        {
            using IServiceScope scope = _provider.CreateScope();
            TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            Concretes.ReadRepository<Person> read = new(context);
            Concretes.WriteRepository<Person> write = new(context);

            Assert.Multiple(() =>
            {
                Assert.That(read.Table, Is.SameAs(context));
                Assert.That(((Concretes.BaseReadRepository<Person>)read).Table, Is.SameAs(context));
                Assert.That(((Base.Repository.Abstractions.ITable)read).Table, Is.SameAs(context));
                Assert.That(read._dbContext, Is.SameAs(((Concretes.BaseReadRepository<Person>)read)._dbContext));
                Assert.That(write.Table, Is.SameAs(context));
                Assert.That(((Concretes.BaseWriteRepository<Person>)write).Table, Is.SameAs(context));
                Assert.That(((Base.Repository.Abstractions.ITable)write).Table, Is.SameAs(context));
                Assert.That(write._dbContext, Is.SameAs(((Concretes.BaseWriteRepository<Person>)write)._dbContext));
            });
        }

        [Test]
        public void DifferentScopes_GetDifferentContexts()
        {
            using IServiceScope first = _provider.CreateScope();
            using IServiceScope second = _provider.CreateScope();

            IReadRepository<Person> firstRepository = first.ServiceProvider.GetRequiredService<IReadRepository<Person>>();
            IReadRepository<Person> secondRepository = second.ServiceProvider.GetRequiredService<IReadRepository<Person>>();

            Assert.That(firstRepository.Table, Is.Not.SameAs(secondRepository.Table));
        }

        [Test]
        public async Task UnitOfWork_SavesChangesMadeThroughRepository()
        {
            using (IServiceScope scope = _provider.CreateScope())
            {
                IUnitOfWork<Person> unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<Person>>();
                await unitOfWork.GetWriteRepository().AddAsync(new Person { Name = "Margaret", Age = 88 });

                Assert.That(await unitOfWork.SaveChangesAsync(), Is.True);
            }

            using TestDbContext context = _database.CreateContext();
            Assert.That(context.Persons.Count(), Is.EqualTo(6));
        }

        [Test]
        public void MissingContextRegistration_ThrowsOnResolve()
        {
            ServiceCollection services = new();
            services.AddEfCoreRepository<Person, TestDbContext>();
            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();

            Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IReadRepository<Person>>());
        }

        [Test]
        public void Registration_UsesRequestedLifetime()
        {
            ServiceCollection services = new();

            services.AddEfCoreRepository<Person, TestDbContext>(ServiceLifetime.Transient);

            List<ServiceLifetime> lifetimes = services
                .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments()[0] == typeof(Person))
                .Select(d => d.Lifetime)
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(lifetimes, Has.Count.EqualTo(7));
                Assert.That(lifetimes, Is.All.EqualTo(ServiceLifetime.Transient));
            });
        }
    }
}
