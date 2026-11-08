using System.Data;
using EfCore.Repository.Abstractions;
using EfCore.Repository.Concretes;
using EfCore.Repository.Extensions;
using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class MultiEntityUnitOfWorkTests
    {
        private SqliteTestDatabase _database;

        [SetUp]
        public void SetUp() => _database = new SqliteTestDatabase();

        [TearDown]
        public void TearDown() => _database.Dispose();

        [Test]
        public async Task SaveChangesAsync_TwoEntityTypes_WritesBothInOneCall()
        {
            SaveChangesCounter counter = new();
            await using TestDbContext context = new(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_database.ConnectionString).AddInterceptors(counter).Options);
            UnitOfWork unitOfWork = new(context);

            Person person = new() { Name = "Ada", Age = 36 };
            unitOfWork.GetWriteRepository<Person>().Add(person);
            unitOfWork.GetWriteRepository<Tag>().Add(new Tag { Code = "t1", Label = "one" });

            bool saved = await unitOfWork.SaveChangesAsync();

            await using TestDbContext verify = _database.CreateContext();
            Assert.Multiple(() =>
            {
                Assert.That(saved, Is.True);
                Assert.That(counter.Calls, Is.EqualTo(1));
                Assert.That(verify.Persons.Count(), Is.EqualTo(1));
                Assert.That(verify.Tags.Count(), Is.EqualTo(1));
            });
        }

        [Test]
        public async Task SaveChangesAsync_NothingChanged_ReturnsFalse()
        {
            await using TestDbContext context = _database.CreateContext();

            Assert.That(await new UnitOfWork(context).SaveChangesAsync(), Is.False);
        }

        [Test]
        public async Task Repositories_UseTheUnitOfWorkContext()
        {
            await using TestDbContext context = _database.CreateContext();
            IUnitOfWork unitOfWork = new UnitOfWork(context);

            Assert.Multiple(() =>
            {
                Assert.That(unitOfWork.GetReadRepository<Person>().Table, Is.SameAs(context));
                Assert.That(unitOfWork.GetWriteRepository<Person>().Table, Is.SameAs(context));
                Assert.That(unitOfWork.GetReadRepository<Basket>().Table, Is.SameAs(context));
                Assert.That(unitOfWork.GetWriteRepository<Tag>().Table, Is.SameAs(context));
            });
            Assert.That(await unitOfWork.GetReadRepository<Person>().CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public void Constructor_NullContext_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new UnitOfWork(null));
        }

        [Test]
        public async Task SaveChangesAsync_CancelledToken_ThrowsAndWritesNothing()
        {
            await using TestDbContext context = _database.CreateContext();
            UnitOfWork unitOfWork = new(context);
            unitOfWork.GetWriteRepository<Tag>().Add(new Tag { Code = "t1", Label = "one" });
            using CancellationTokenSource cts = new();
            cts.Cancel();

            Assert.CatchAsync<OperationCanceledException>(() => unitOfWork.SaveChangesAsync(cts.Token));

            await using TestDbContext verify = _database.CreateContext();
            Assert.That(verify.Tags.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task ExecuteInTransactionAsync_TwoEntityTypes_CommitsBoth()
        {
            await using TestDbContext context = _database.CreateContext();
            UnitOfWork unitOfWork = new(context);

            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await unitOfWork.GetWriteRepository<Person>().AddAsync(new Person { Name = "Ada", Age = 36 }, ct);
                await unitOfWork.SaveChangesAsync(ct);
                await unitOfWork.GetWriteRepository<Tag>().AddAsync(new Tag { Code = "t1", Label = "one" }, ct);
            });

            await using TestDbContext verify = _database.CreateContext();
            Assert.Multiple(() =>
            {
                Assert.That(verify.Persons.Count(), Is.EqualTo(1));
                Assert.That(verify.Tags.Count(), Is.EqualTo(1));
                Assert.That(context.Database.CurrentTransaction, Is.Null);
            });
        }

        [Test]
        public async Task ExecuteInTransactionAsync_OperationThrowsAfterSave_RollsBackEverything()
        {
            await using TestDbContext context = _database.CreateContext();
            UnitOfWork unitOfWork = new(context);

            Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await unitOfWork.GetWriteRepository<Person>().AddAsync(new Person { Name = "Ada", Age = 36 }, ct);
                await unitOfWork.SaveChangesAsync(ct);
                await unitOfWork.GetWriteRepository<Tag>().AddAsync(new Tag { Code = "t1", Label = "one" }, ct);
                throw new InvalidOperationException("boom");
            }));

            bool savedAfterFailure = await unitOfWork.SaveChangesAsync();

            await using TestDbContext verify = _database.CreateContext();
            Assert.Multiple(() =>
            {
                Assert.That(verify.Persons.Count(), Is.EqualTo(0));
                Assert.That(verify.Tags.Count(), Is.EqualTo(0));
                Assert.That(savedAfterFailure, Is.False);
                Assert.That(context.ChangeTracker.Entries().Count(), Is.EqualTo(0));
                Assert.That(context.Database.CurrentTransaction, Is.Null);
            });
        }

        [Test]
        public async Task ExecuteInTransactionAsync_FailingSave_RollsBackEarlierSave()
        {
            await using TestDbContext context = _database.CreateContext();
            UnitOfWork unitOfWork = new(context);

            Assert.CatchAsync<DbUpdateException>(() => unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await unitOfWork.GetWriteRepository<Tag>().AddAsync(new Tag { Code = "t1", Label = "one" }, ct);
                await unitOfWork.SaveChangesAsync(ct);
                unitOfWork.GetWriteRepository<Person>().Add(new Person { Name = "Ada", Age = 36 });
                unitOfWork.GetWriteRepository<Basket>().Add(new Basket { Description = "orphan", PersonId = 999 });
            }));

            await using TestDbContext verify = _database.CreateContext();
            Assert.Multiple(() =>
            {
                Assert.That(verify.Tags.Count(), Is.EqualTo(0));
                Assert.That(verify.Persons.Count(), Is.EqualTo(0));
                Assert.That(verify.Baskets.Count(), Is.EqualTo(0));
            });
        }

        [Test]
        public async Task ExecuteInTransactionAsync_CancelledToken_ThrowsAndWritesNothing()
        {
            await using TestDbContext context = _database.CreateContext();
            UnitOfWork unitOfWork = new(context);
            bool operationRan = false;
            using CancellationTokenSource cts = new();
            cts.Cancel();

            Assert.CatchAsync<OperationCanceledException>(() => unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                operationRan = true;
                await unitOfWork.GetWriteRepository<Tag>().AddAsync(new Tag { Code = "t1", Label = "one" }, ct);
            }, IsolationLevel.Unspecified, cts.Token));

            await using TestDbContext verify = _database.CreateContext();
            Assert.Multiple(() =>
            {
                Assert.That(operationRan, Is.False);
                Assert.That(verify.Tags.Count(), Is.EqualTo(0));
            });
        }

        [Test]
        public async Task ExecuteInTransactionAsync_TokenCancelledDuringOperation_RollsBack()
        {
            await using TestDbContext context = _database.CreateContext();
            UnitOfWork unitOfWork = new(context);
            using CancellationTokenSource cts = new();

            Assert.CatchAsync<OperationCanceledException>(() => unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await unitOfWork.GetWriteRepository<Tag>().AddAsync(new Tag { Code = "t1", Label = "one" }, ct);
                await unitOfWork.SaveChangesAsync(ct);
                cts.Cancel();
                ct.ThrowIfCancellationRequested();
            }, IsolationLevel.Unspecified, cts.Token));

            await using TestDbContext verify = _database.CreateContext();
            Assert.That(verify.Tags.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task ExecuteInTransactionAsync_IsolationLevel_IsAppliedToTheTransaction()
        {
            await using TestDbContext context = _database.CreateContext();
            UnitOfWork unitOfWork = new(context);
            IsolationLevel? seen = null;

            await unitOfWork.ExecuteInTransactionAsync(ct =>
            {
                seen = context.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel;
                return Task.CompletedTask;
            }, IsolationLevel.ReadUncommitted);

            Assert.That(seen, Is.EqualTo(IsolationLevel.ReadUncommitted));
        }

        [Test]
        public async Task ExecuteInTransactionAsync_InsideCallerTransaction_JoinsIt()
        {
            await using TestDbContext context = _database.CreateContext();
            UnitOfWork unitOfWork = new(context);
            await using IDbContextTransaction outer = await context.Database.BeginTransactionAsync();

            await unitOfWork.ExecuteInTransactionAsync(async ct =>
                await unitOfWork.GetWriteRepository<Tag>().AddAsync(new Tag { Code = "t1", Label = "one" }, ct));

            Assert.That(context.Database.CurrentTransaction, Is.SameAs(outer));
            await outer.RollbackAsync();

            await using TestDbContext verify = _database.CreateContext();
            Assert.That(verify.Tags.Count(), Is.EqualTo(0));
        }

        [Test]
        public void ExecuteInTransactionAsync_NullOperation_Throws()
        {
            using TestDbContext context = _database.CreateContext();

            Assert.ThrowsAsync<ArgumentNullException>(() => new UnitOfWork(context).ExecuteInTransactionAsync(null));
        }

        [Test]
        public async Task ExecuteInTransactionAsync_RunsThroughTheProviderExecutionStrategy()
        {
            await using TestDbContext context = new(new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_database.ConnectionString, sqlite => sqlite.ExecutionStrategy(dependencies => new RetryOnceStrategy(dependencies)))
                .Options);
            UnitOfWork unitOfWork = new(context);
            int attempts = 0;

            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                attempts++;
                await unitOfWork.GetWriteRepository<Tag>().AddAsync(new Tag { Code = "t" + attempts, Label = "attempt" }, ct);
                await unitOfWork.SaveChangesAsync(ct);
                if (attempts == 1)
                {
                    throw new TransientTestException();
                }
            });

            await using TestDbContext verify = _database.CreateContext();
            Assert.Multiple(() =>
            {
                Assert.That(attempts, Is.EqualTo(2));
                Assert.That(verify.Tags.Select(t => t.Code).ToList(), Is.EqualTo(new[] { "t2" }));
            });
        }

        [Test]
        public async Task AddEfCoreRepository_ResolvesUnitOfWorkOnTheScopedContext()
        {
            ServiceCollection services = new();
            services.AddDbContext<TestDbContext>(_database.ConfigureContext);
            services.AddEfCoreRepository<Person, TestDbContext>();
            services.AddEfCoreRepository<Tag, TestDbContext>();
            await using ServiceProvider provider = services.BuildServiceProvider();

            using IServiceScope scope = provider.CreateScope();
            using IServiceScope otherScope = provider.CreateScope();
            TestDbContext context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            Assert.Multiple(() =>
            {
                Assert.That(services.Count(d => d.ServiceType == typeof(IUnitOfWork)), Is.EqualTo(1));
                Assert.That(services.Single(d => d.ServiceType == typeof(IUnitOfWork)).Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
                Assert.That(scope.ServiceProvider.GetRequiredService<IUnitOfWork>(), Is.SameAs(unitOfWork));
                Assert.That(otherScope.ServiceProvider.GetRequiredService<IUnitOfWork>(), Is.Not.SameAs(unitOfWork));
                Assert.That(unitOfWork.GetReadRepository<Person>().Table, Is.SameAs(context));
                Assert.That(unitOfWork.GetWriteRepository<Tag>().Table, Is.SameAs(context));
                Assert.That(scope.ServiceProvider.GetRequiredService<IWriteRepository<Person>>().Table, Is.SameAs(context));
            });

            scope.ServiceProvider.GetRequiredService<IWriteRepository<Person>>().Add(new Person { Name = "Ada", Age = 36 });
            unitOfWork.GetWriteRepository<Tag>().Add(new Tag { Code = "t1", Label = "one" });
            Assert.That(await unitOfWork.SaveChangesAsync(), Is.True);

            await using TestDbContext verify = _database.CreateContext();
            Assert.Multiple(() =>
            {
                Assert.That(verify.Persons.Count(), Is.EqualTo(1));
                Assert.That(verify.Tags.Count(), Is.EqualTo(1));
            });
        }

        [Test]
        public async Task GenericUnitOfWork_SaveChangesAsyncWithToken_Saves()
        {
            await using TestDbContext context = _database.CreateContext();
            UnitOfWork<Tag> unitOfWork = new(context);
            unitOfWork.GetWriteRepository().Add(new Tag { Code = "t1", Label = "one" });

            Assert.That(await unitOfWork.SaveChangesAsync(CancellationToken.None), Is.True);

            unitOfWork.GetWriteRepository().Add(new Tag { Code = "t2", Label = "two" });
            using CancellationTokenSource cts = new();
            cts.Cancel();
            Assert.CatchAsync<OperationCanceledException>(() => unitOfWork.SaveChangesAsync(cts.Token));

            await using TestDbContext verify = _database.CreateContext();
            Assert.That(verify.Tags.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task BaseWriteRepository_SaveChangesAsyncWithToken_Saves()
        {
            await using TestDbContext context = _database.CreateContext();
            BaseWriteRepository<Tag> repository = new(context);
            await repository.AddAsync(new Tag { Code = "t1", Label = "one" });

            Assert.That(await repository.SaveChangesAsync(CancellationToken.None), Is.True);

            await repository.AddAsync(new Tag { Code = "t2", Label = "two" });
            using CancellationTokenSource cts = new();
            cts.Cancel();
            Assert.CatchAsync<OperationCanceledException>(() => repository.SaveChangesAsync(cts.Token));

            await using TestDbContext verify = _database.CreateContext();
            Assert.That(verify.Tags.Count(), Is.EqualTo(1));
        }

        private sealed class TransientTestException : Exception
        {
        }

        private sealed class RetryOnceStrategy : ExecutionStrategy
        {
            public RetryOnceStrategy(ExecutionStrategyDependencies dependencies)
                : base(dependencies, 1, TimeSpan.Zero)
            {
            }

            protected override bool ShouldRetryOn(Exception exception) => exception is TransientTestException;
        }
    }
}
