using EfCore.Repository.Abstractions;
using EfCore.Repository.Concretes;
using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class BulkWriteTests
    {
        private sealed class NonQueryCounter : DbCommandInterceptor
        {
            public int Calls { get; private set; }

            public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                Calls++;
                return new ValueTask<InterceptionResult<int>>(result);
            }
        }

        private SqliteTestDatabase _database;

        [SetUp]
        public void SetUp() => _database = new SqliteTestDatabase();

        [TearDown]
        public void TearDown() => _database.Dispose();

        private void SeedPersons()
            => _database.Seed(
                new Person { Name = "Ada", Age = 36 },
                new Person { Name = "Alan", Age = 41 },
                new Person { Name = "Grace", Age = 85 },
                new Person { Name = "Linus", Age = 28 },
                new Person { Name = "Barbara", Age = 52 });

        private Dictionary<string, int> AgesByName()
        {
            using TestDbContext context = _database.CreateContext();
            return context.Persons.AsNoTracking().ToDictionary(p => p.Name, p => p.Age);
        }

        private static readonly Dictionary<string, int> SeededAges = new()
        {
            ["Ada"] = 36,
            ["Alan"] = 41,
            ["Grace"] = 85,
            ["Linus"] = 28,
            ["Barbara"] = 52
        };

        [Test]
        public async Task DeleteWhereAsync_DeletesMatchingRowsAndReturnsTheirCount()
        {
            SeedPersons();
            await using TestDbContext context = _database.CreateContext();
            IWriteRepository<Person> repository = new WriteRepository<Person>(context);

            int deleted = await repository.DeleteWhereAsync(p => p.Age > 40);

            Assert.Multiple(() =>
            {
                Assert.That(deleted, Is.EqualTo(3));
                Assert.That(AgesByName(), Is.EquivalentTo(new Dictionary<string, int> { ["Ada"] = 36, ["Linus"] = 28 }));
            });
        }

        [Test]
        public async Task DeleteWhereAsync_NoMatch_ReturnsZeroAndKeepsEveryRow()
        {
            SeedPersons();
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<Person> repository = new(context);

            int deleted = await repository.DeleteWhereAsync(p => p.Age > 100);

            Assert.That(deleted, Is.EqualTo(0));
            Assert.That(AgesByName(), Is.EquivalentTo(SeededAges));
        }

        [Test]
        public async Task UpdateWhereAsync_UpdatesOnlyMatchingRowsAndReturnsTheirCount()
        {
            SeedPersons();
            await using TestDbContext context = _database.CreateContext();
            IWriteRepository<Person> repository = new WriteRepository<Person>(context);

            int updated = await repository.UpdateWhereAsync(p => p.Age < 40, s => s.SetProperty(p => p.Age, p => p.Age + 1));

            Assert.Multiple(() =>
            {
                Assert.That(updated, Is.EqualTo(2));
                Assert.That(AgesByName(), Is.EquivalentTo(new Dictionary<string, int>
                {
                    ["Ada"] = 37,
                    ["Alan"] = 41,
                    ["Grace"] = 85,
                    ["Linus"] = 29,
                    ["Barbara"] = 52
                }));
            });
        }

        [Test]
        public async Task UpdateWhereAsync_SeveralSetters_AreAppliedInOneCommand()
        {
            SeedPersons();
            NonQueryCounter counter = new();
            await using TestDbContext context = new(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_database.ConnectionString).AddInterceptors(counter).Options);
            IWriteRepository<Person> repository = new WriteRepository<Person>(context);

            int updated = await repository.UpdateWhereAsync(
                p => p.Name == "Grace",
                s => s.SetProperty(p => p.Name, "Grace Hopper").SetProperty(p => p.Age, 86));

            await using TestDbContext verify = _database.CreateContext();
            Person grace = verify.Persons.Single(p => p.Name == "Grace Hopper");
            Assert.Multiple(() =>
            {
                Assert.That(updated, Is.EqualTo(1));
                Assert.That(grace.Age, Is.EqualTo(86));
                Assert.That(counter.Calls, Is.EqualTo(1));
                Assert.That(verify.Persons.Count(), Is.EqualTo(5));
            });
        }

        [Test]
        public async Task BulkOperations_BypassTheChangeTrackerAndDoNotSave()
        {
            SeedPersons();
            SaveChangesCounter counter = new();
            await using TestDbContext context = new(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_database.ConnectionString).AddInterceptors(counter).Options);
            IWriteRepository<Person> repository = new WriteRepository<Person>(context);
            Person ada = context.Persons.Single(p => p.Name == "Ada");
            repository.Add(new Person { Name = "Pending", Age = 1 });

            int updated = await repository.UpdateWhereAsync(p => p.Name == "Ada", s => s.SetProperty(p => p.Age, 99));
            int deleted = await repository.DeleteWhereAsync(p => p.Name == "Linus");

            Assert.Multiple(() =>
            {
                Assert.That(updated, Is.EqualTo(1));
                Assert.That(deleted, Is.EqualTo(1));
                Assert.That(counter.Calls, Is.EqualTo(0));
                Assert.That(ada.Age, Is.EqualTo(36));
                Assert.That(context.Entry(ada).State, Is.EqualTo(EntityState.Unchanged));
                Assert.That(AgesByName()["Ada"], Is.EqualTo(99));
                Assert.That(AgesByName().ContainsKey("Pending"), Is.False);
                Assert.That(AgesByName().ContainsKey("Linus"), Is.False);
            });
        }

        [Test]
        public async Task BulkOperations_InsideRolledBackTransaction_AreUndone()
        {
            SeedPersons();
            await using TestDbContext context = _database.CreateContext();
            IWriteRepository<Person> repository = new WriteRepository<Person>(context);

            await using (var transaction = await repository.BeginTransactionAsync())
            {
                Assert.That(await repository.DeleteWhereAsync(p => p.Age > 40), Is.EqualTo(3));
                Assert.That(await repository.UpdateWhereAsync(p => p.Age < 40, s => s.SetProperty(p => p.Age, 0)), Is.EqualTo(2));
                Assert.That(await context.Persons.CountAsync(), Is.EqualTo(2));
                await transaction.RollbackAsync();
            }

            Assert.That(AgesByName(), Is.EquivalentTo(SeededAges));
        }

        [Test]
        public async Task BulkOperations_InsideCommittedTransaction_AreWritten()
        {
            SeedPersons();
            await using TestDbContext context = _database.CreateContext();
            IWriteRepository<Person> repository = new WriteRepository<Person>(context);

            await using (var transaction = await repository.BeginTransactionAsync())
            {
                await repository.DeleteWhereAsync(p => p.Name == "Grace");
                await transaction.CommitAsync();
            }

            Assert.That(AgesByName().ContainsKey("Grace"), Is.False);
        }

        [Test]
        public async Task BulkOperations_InUnitOfWorkTransactionThatFails_AreRolledBack()
        {
            SeedPersons();
            await using TestDbContext context = _database.CreateContext();
            IUnitOfWork unitOfWork = new UnitOfWork(context);

            Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                IWriteRepository<Person> repository = unitOfWork.GetWriteRepository<Person>();
                await repository.UpdateWhereAsync(p => p.Age > 0, s => s.SetProperty(p => p.Age, 1), ct);
                await repository.DeleteWhereAsync(p => p.Name == "Ada", ct);
                throw new InvalidOperationException("fail after bulk operations");
            }));

            Assert.That(AgesByName(), Is.EquivalentTo(SeededAges));
        }

        [Test]
        public async Task BulkOperations_InUnitOfWorkTransaction_AreCommittedWithTrackedChanges()
        {
            SeedPersons();
            await using TestDbContext context = _database.CreateContext();
            IUnitOfWork unitOfWork = new UnitOfWork(context);

            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await unitOfWork.GetWriteRepository<Person>().DeleteWhereAsync(p => p.Age > 80, ct);
                unitOfWork.GetWriteRepository<Tag>().Add(new Tag { Code = "t1", Label = "one" });
            });

            await using TestDbContext verify = _database.CreateContext();
            Assert.Multiple(() =>
            {
                Assert.That(AgesByName().ContainsKey("Grace"), Is.False);
                Assert.That(AgesByName().Count, Is.EqualTo(4));
                Assert.That(verify.Tags.Count(), Is.EqualTo(1));
            });
        }

        [Test]
        public async Task BulkOperations_RespectGlobalQueryFilters()
        {
            await using SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            DbContextOptions<FilteredDbContext> options = new DbContextOptionsBuilder<FilteredDbContext>().UseSqlite(connection).Options;
            await using (FilteredDbContext seed = new(options))
            {
                await seed.Database.EnsureCreatedAsync();
                seed.Items.AddRange(
                    new ArchivableItem { Name = "active-1", Stock = 1 },
                    new ArchivableItem { Name = "active-2", Stock = 2 },
                    new ArchivableItem { Name = "archived", Stock = 3, IsArchived = true });
                await seed.SaveChangesAsync();
            }

            await using FilteredDbContext context = new(options);
            IWriteRepository<ArchivableItem> repository = new WriteRepository<ArchivableItem>(context);

            int updated = await repository.UpdateWhereAsync(_ => true, s => s.SetProperty(i => i.Stock, 0));
            int deleted = await repository.DeleteWhereAsync(_ => true);

            await using FilteredDbContext verify = new(options);
            List<ArchivableItem> remaining = await verify.Items.IgnoreQueryFilters().ToListAsync();
            Assert.Multiple(() =>
            {
                Assert.That(updated, Is.EqualTo(2));
                Assert.That(deleted, Is.EqualTo(2));
                Assert.That(remaining.Select(i => i.Name), Is.EqualTo(new[] { "archived" }));
                Assert.That(remaining.Single().Stock, Is.EqualTo(3));
            });
        }

        [Test]
        public async Task DeleteWhereAsync_Specification_CombinesEveryCondition()
        {
            SeedPersons();
            await using TestDbContext context = _database.CreateContext();
            IWriteRepository<Person> repository = new WriteRepository<Person>(context);
            SpecificationBase<Person> specification = new()
            {
                Conditions = { p => p.Age > 30, p => p.Name.StartsWith("A") }
            };

            int deleted = await repository.DeleteWhereAsync(specification);

            Assert.That(deleted, Is.EqualTo(2));
            Assert.That(AgesByName().Keys, Is.EquivalentTo(new[] { "Grace", "Linus", "Barbara" }));
        }

        [Test]
        public async Task UpdateWhereAsync_Specification_UpdatesOnlyMatchingRows()
        {
            SeedPersons();
            await using TestDbContext context = _database.CreateContext();
            IWriteRepository<Person> repository = new WriteRepository<Person>(context);
            Specification<Person> specification = new()
            {
                Conditions = { p => p.Age > 50 },
                OrderBy = q => q.OrderBy(p => p.Name),
                AsNoTracking = true
            };

            int updated = await repository.UpdateWhereAsync(specification, s => s.SetProperty(p => p.Age, 50));

            Assert.That(updated, Is.EqualTo(2));
            Assert.That(AgesByName(), Is.EquivalentTo(new Dictionary<string, int>
            {
                ["Ada"] = 36,
                ["Alan"] = 41,
                ["Grace"] = 50,
                ["Linus"] = 28,
                ["Barbara"] = 50
            }));
        }

        [Test]
        public async Task BulkOperations_SpecificationThatCannotLimitTheRows_ThrowAndChangeNothing()
        {
            SeedPersons();
            await using TestDbContext context = _database.CreateContext();
            IWriteRepository<Person> repository = new WriteRepository<Person>(context);

            SpecificationBase<Person>[] invalid =
            {
                new SpecificationBase<Person>(),
                new SpecificationBase<Person> { Conditions = null },
                new SpecificationBase<Person> { Conditions = { null } },
                new Specification<Person> { Conditions = { p => p.Age > 0 }, Take = 1 },
                new Specification<Person> { Conditions = { p => p.Age > 0 }, Skip = 1 },
                new PaginationSpecification<Person> { Conditions = { p => p.Age > 0 }, PageIndex = 1, PageSize = 1 }
            };

            foreach (SpecificationBase<Person> specification in invalid)
            {
                Assert.ThrowsAsync<ArgumentException>(() => repository.DeleteWhereAsync(specification));
                Assert.ThrowsAsync<ArgumentException>(() => repository.UpdateWhereAsync(specification, s => s.SetProperty(p => p.Age, 0)));
            }

            Assert.That(AgesByName(), Is.EquivalentTo(SeededAges));
        }

        [Test]
        public async Task BulkOperations_NullArguments_Throw()
        {
            await using TestDbContext context = _database.CreateContext();
            IWriteRepository<Person> repository = new WriteRepository<Person>(context);
            IWriteRepository<Person> missing = null;

            Assert.Multiple(() =>
            {
                Assert.ThrowsAsync<ArgumentNullException>(() => missing.DeleteWhereAsync(p => p.Age > 0));
                Assert.ThrowsAsync<ArgumentNullException>(() => missing.UpdateWhereAsync(p => p.Age > 0, s => s.SetProperty(p => p.Age, 0)));
                Assert.ThrowsAsync<ArgumentNullException>(() => repository.DeleteWhereAsync((System.Linq.Expressions.Expression<Func<Person, bool>>)null));
                Assert.ThrowsAsync<ArgumentNullException>(() => repository.DeleteWhereAsync((SpecificationBase<Person>)null));
                Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateWhereAsync((System.Linq.Expressions.Expression<Func<Person, bool>>)null, s => s.SetProperty(p => p.Age, 0)));
                Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateWhereAsync((SpecificationBase<Person>)null, s => s.SetProperty(p => p.Age, 0)));
                Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateWhereAsync(p => p.Age > 0, null));
                Assert.ThrowsAsync<ArgumentNullException>(() => repository.UpdateWhereAsync(new SpecificationBase<Person> { Conditions = { p => p.Age > 0 } }, null));
            });
        }

        [Test]
        public async Task BulkOperations_CancelledToken_ThrowAndChangeNothing()
        {
            SeedPersons();
            await using TestDbContext context = _database.CreateContext();
            IWriteRepository<Person> repository = new WriteRepository<Person>(context);
            using CancellationTokenSource cancelled = new();
            cancelled.Cancel();

            Assert.CatchAsync<OperationCanceledException>(() => repository.DeleteWhereAsync(p => p.Age > 0, cancelled.Token));
            Assert.CatchAsync<OperationCanceledException>(() => repository.UpdateWhereAsync(p => p.Age > 0, s => s.SetProperty(p => p.Age, 0), cancelled.Token));

            Assert.That(AgesByName(), Is.EquivalentTo(SeededAges));
        }

        [Test]
        public async Task DeleteWhereAsync_CompositeKeyEntity_DeletesMatchingRows()
        {
            await using (TestDbContext seed = _database.CreateContext())
            {
                seed.OrderLines.AddRange(
                    new OrderLine { OrderId = 1, LineNumber = 1, Product = "mouse" },
                    new OrderLine { OrderId = 1, LineNumber = 2, Product = "keyboard" },
                    new OrderLine { OrderId = 2, LineNumber = 1, Product = "screen" });
                await seed.SaveChangesAsync();
            }

            await using TestDbContext context = _database.CreateContext();
            IWriteRepository<OrderLine> repository = new UnitOfWork(context).GetWriteRepository<OrderLine>();

            int deleted = await repository.DeleteWhereAsync(l => l.OrderId == 1);

            await using TestDbContext verify = _database.CreateContext();
            Assert.That(deleted, Is.EqualTo(2));
            Assert.That(verify.OrderLines.Select(l => l.Product), Is.EqualTo(new[] { "screen" }));
        }
    }
}
