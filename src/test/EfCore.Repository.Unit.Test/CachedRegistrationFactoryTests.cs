using System.Reflection;
using Base.Repository.Abstractions;
using EfCore.Repository.Abstractions;
using EfCore.Repository.Concretes;
using EfCore.Repository.Extensions;
using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class CachedRegistrationFactoryTests
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

        private ServiceProvider BuildProvider<TBaseType>(ServiceLifetime lifetime)
        {
            ServiceCollection services = new();
            services.AddDbContext<TestDbContext>(_database.ConfigureContext);
            services.EfCoreRepositoryServiceRegistration<TBaseType, TestDbContext>(lifetime, typeof(CachedRegistrationFactoryTests).Assembly);
            return services.BuildServiceProvider();
        }

        [Test]
        public void AssemblyRegistration_ResolvesTheSameConcreteTypesAsBefore()
        {
            using ServiceProvider provider = BuildProvider<ITestEntity>(ServiceLifetime.Scoped);
            using IServiceScope scope = provider.CreateScope();
            IServiceProvider sp = scope.ServiceProvider;

            Assert.Multiple(() =>
            {
                Assert.That(sp.GetRequiredService<IBaseReadRepository<Person>>(), Is.TypeOf<BaseReadRepository<Person>>());
                Assert.That(sp.GetRequiredService<IBaseWriteRepository<Person>>(), Is.TypeOf<BaseWriteRepository<Person>>());
                Assert.That(sp.GetRequiredService<IReadRepository<Person>>(), Is.TypeOf<ReadRepository<Person>>());
                Assert.That(sp.GetRequiredService<IWriteRepository<Person>>(), Is.TypeOf<WriteRepository<Person>>());
                Assert.That(sp.GetRequiredService<IDbReadRepository<Person>>(), Is.TypeOf<DbReadRepository<Person>>());
                Assert.That(sp.GetRequiredService<IDbWriteRepository<Person>>(), Is.TypeOf<DbWriteRepository<Person>>());
                Assert.That(sp.GetRequiredService<IUnitOfWork<Person>>(), Is.TypeOf<UnitOfWork<Person>>());
                Assert.That(sp.GetRequiredService<IReadRepository<Basket>>(), Is.TypeOf<ReadRepository<Basket>>());
                Assert.That(sp.GetRequiredService<IUnitOfWork<OrderLine>>(), Is.TypeOf<UnitOfWork<OrderLine>>());
            });
        }

        [Test]
        public void AssemblyRegistration_EveryServiceUsesTheScopeContextAndProvider()
        {
            using ServiceProvider provider = BuildProvider<ITestEntity>(ServiceLifetime.Scoped);
            using IServiceScope scope = provider.CreateScope();
            IServiceProvider sp = scope.ServiceProvider;
            TestDbContext context = sp.GetRequiredService<TestDbContext>();

            ITable[] tables =
            {
                sp.GetRequiredService<IBaseReadRepository<Person>>(),
                sp.GetRequiredService<IBaseWriteRepository<Person>>(),
                sp.GetRequiredService<IReadRepository<Person>>(),
                sp.GetRequiredService<IWriteRepository<Person>>(),
                sp.GetRequiredService<IDbReadRepository<Person>>(),
                sp.GetRequiredService<IDbWriteRepository<Person>>(),
                sp.GetRequiredService<IUnitOfWork<Person>>().GetTable(),
            };
            UnitOfWork<Person> unitOfWork = (UnitOfWork<Person>)sp.GetRequiredService<IUnitOfWork<Person>>();

            Assert.Multiple(() =>
            {
                foreach (ITable table in tables)
                {
                    Assert.That(table.Table, Is.SameAs(context), table.GetType().Name);
                }

                Assert.That(((ReadRepository<Person>)sp.GetRequiredService<IReadRepository<Person>>())._serviceProvider, Is.SameAs(sp));
                Assert.That(unitOfWork._serviceProvider, Is.SameAs(sp));
                Assert.That(unitOfWork.GetReadRepository(), Is.SameAs(sp.GetRequiredService<IReadRepository<Person>>()));
            });
        }

        [Test]
        public void AssemblyRegistration_Scoped_SameInstanceInScopeAndNewOnePerScope()
        {
            using ServiceProvider provider = BuildProvider<ITestEntity>(ServiceLifetime.Scoped);
            using IServiceScope first = provider.CreateScope();
            using IServiceScope second = provider.CreateScope();

            IReadRepository<Person> firstRead = first.ServiceProvider.GetRequiredService<IReadRepository<Person>>();
            IUnitOfWork<Person> firstUnitOfWork = first.ServiceProvider.GetRequiredService<IUnitOfWork<Person>>();
            IReadRepository<Person> secondRead = second.ServiceProvider.GetRequiredService<IReadRepository<Person>>();

            Assert.Multiple(() =>
            {
                Assert.That(first.ServiceProvider.GetRequiredService<IReadRepository<Person>>(), Is.SameAs(firstRead));
                Assert.That(first.ServiceProvider.GetRequiredService<IUnitOfWork<Person>>(), Is.SameAs(firstUnitOfWork));
                Assert.That(secondRead, Is.Not.SameAs(firstRead));
                Assert.That(secondRead.Table, Is.Not.SameAs(firstRead.Table));
                Assert.That(secondRead.Table, Is.SameAs(second.ServiceProvider.GetRequiredService<TestDbContext>()));
            });
        }

        [Test]
        public void AssemblyRegistration_Transient_NewInstanceEveryResolveOnTheScopeContext()
        {
            using ServiceProvider provider = BuildProvider<ITestEntity>(ServiceLifetime.Transient);
            using IServiceScope scope = provider.CreateScope();
            IServiceProvider sp = scope.ServiceProvider;

            IWriteRepository<Person> first = sp.GetRequiredService<IWriteRepository<Person>>();
            IWriteRepository<Person> second = sp.GetRequiredService<IWriteRepository<Person>>();
            IUnitOfWork<Person> firstUnitOfWork = sp.GetRequiredService<IUnitOfWork<Person>>();
            IUnitOfWork<Person> secondUnitOfWork = sp.GetRequiredService<IUnitOfWork<Person>>();

            Assert.Multiple(() =>
            {
                Assert.That(second, Is.Not.SameAs(first));
                Assert.That(secondUnitOfWork, Is.Not.SameAs(firstUnitOfWork));
                Assert.That(second.Table, Is.SameAs(first.Table));
                Assert.That(firstUnitOfWork.GetTable().Table, Is.SameAs(first.Table));
                Assert.That(first.Table, Is.SameAs(sp.GetRequiredService<TestDbContext>()));
            });
        }

        [Test]
        public async Task AssemblyRegistration_RepeatedResolves_KeepWorkingOnTheDatabase()
        {
            using ServiceProvider provider = BuildProvider<ITestEntity>(ServiceLifetime.Transient);
            using IServiceScope scope = provider.CreateScope();
            IServiceProvider sp = scope.ServiceProvider;

            for (int i = 0; i < 3; i++)
            {
                await sp.GetRequiredService<IWriteRepository<Person>>().AddAsync(new Person { Name = $"P{i}", Age = 20 + i });
            }

            bool saved = await sp.GetRequiredService<IUnitOfWork<Person>>().SaveChangesAsync();

            Assert.Multiple(async () =>
            {
                Assert.That(saved, Is.True);
                Assert.That(await sp.GetRequiredService<IReadRepository<Person>>().CountAsync(), Is.EqualTo(8));
            });
        }

        [Test]
        public void AssemblyRegistration_ContextConstructorEntity_GetsTheScopeContextAndProvider()
        {
            using ServiceProvider provider = BuildProvider<IFactoryMarker>(ServiceLifetime.Transient);
            using IServiceScope scope = provider.CreateScope();
            IServiceProvider sp = scope.ServiceProvider;

            CountingContextEntity first = (CountingContextEntity)sp.GetRequiredService<IFactoryMarker>();
            CountingContextEntity second = (CountingContextEntity)sp.GetRequiredService<IFactoryMarker>();

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.Not.SameAs(second));
                Assert.That(first.Context, Is.SameAs(sp.GetRequiredService<TestDbContext>()));
                Assert.That(first.ServiceProvider, Is.SameAs(sp));
                Assert.That(second.Context, Is.SameAs(first.Context));
            });
        }

        [Test]
        public void AssemblyRegistration_ConstructorThatThrows_IsReportedLikeBefore()
        {
            using ServiceProvider provider = BuildProvider<IThrowingMarker>(ServiceLifetime.Scoped);
            using IServiceScope scope = provider.CreateScope();

            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => scope.ServiceProvider.GetRequiredService<IThrowingMarker>());

            Assert.Multiple(() =>
            {
                Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
                Assert.That(exception.InnerException.Message, Is.EqualTo(ThrowingContextEntity.Message));
            });
        }

        [Test]
        public void AssemblyRegistration_MissingContext_StillFailsWhenResolving()
        {
            ServiceCollection services = new();
            services.EfCoreRepositoryServiceRegistration<ITestEntity, TestDbContext>(ServiceLifetime.Scoped, typeof(ITestEntity).Assembly);
            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();

            Assert.Multiple(() =>
            {
                Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IReadRepository<Person>>());
                Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IUnitOfWork<Person>>());
            });
        }

        public interface IFactoryMarker
        {
        }

        public class CountingContextEntity : IFactoryMarker
        {
            public CountingContextEntity()
            {
            }

            public CountingContextEntity(TestDbContext context, IServiceProvider serviceProvider)
            {
                Context = context;
                ServiceProvider = serviceProvider;
            }

            public TestDbContext Context { get; }

            public IServiceProvider ServiceProvider { get; }
        }

        public interface IThrowingMarker
        {
        }

        public class ThrowingContextEntity : IThrowingMarker
        {
            public const string Message = "constructor failed";

            public ThrowingContextEntity()
            {
            }

            public ThrowingContextEntity(TestDbContext context, IServiceProvider serviceProvider) => throw new InvalidOperationException(Message);
        }
    }
}
