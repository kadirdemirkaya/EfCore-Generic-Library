using EfCore.Repository.Concretes;
using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class WriteAndPaginationRegressionTests
    {
        private SqliteTestDatabase _database;
        private TestDbContext _context;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();
            _context = _database.CreateContext();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
            _database.Dispose();
        }

        private void SeedTags()
        {
            using TestDbContext context = _database.CreateContext();
            context.Tags.Add(new Tag { Code = "c", Label = "beta" });
            context.SaveChanges();
            context.Tags.Add(new Tag { Code = "a", Label = "gamma" });
            context.SaveChanges();
            context.Tags.Add(new Tag { Code = "b", Label = "alpha" });
            context.SaveChanges();
        }

        private void SeedOrderLines()
        {
            using TestDbContext context = _database.CreateContext();
            context.OrderLines.Add(new OrderLine { OrderId = 2, LineNumber = 1, Product = "screen" });
            context.SaveChanges();
            context.OrderLines.Add(new OrderLine { OrderId = 1, LineNumber = 2, Product = "mouse" });
            context.SaveChanges();
            context.OrderLines.Add(new OrderLine { OrderId = 1, LineNumber = 1, Product = "keyboard" });
            context.SaveChanges();
        }

        [Test]
        public async Task InsertAsync_List_CallsSaveChangesOnce()
        {
            SaveChangesCounter counter = new();
            using TestDbContext context = new(new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_database.ConnectionString)
                .AddInterceptors(counter)
                .Options);
            WriteRepository<Person> repository = new(context);

            await repository.InsertAsync(new List<Person> { new() { Name = "Ada" }, new() { Name = "Alan" }, new() { Name = "Grace" } });

            Assert.That(counter.Calls, Is.EqualTo(1));
        }

        [Test]
        public async Task InsertAsync_List_ReturnsKeysInInputOrder()
        {
            WriteRepository<Person> repository = new(_context);
            List<Person> persons = new() { new() { Name = "Ada" }, new() { Name = "Alan" } };

            object[] keys = await repository.InsertAsync(persons);

            Assert.Multiple(() =>
            {
                Assert.That(keys, Has.Length.EqualTo(2));
                Assert.That(((object[])keys[0])[0], Is.EqualTo(persons[0].Id));
                Assert.That(((object[])keys[1])[0], Is.EqualTo(persons[1].Id));
                Assert.That(persons[0].Id, Is.GreaterThan(0));
                Assert.That(persons[1].Id, Is.GreaterThan(0));
            });
        }

        [Test]
        public async Task InsertAsync_List_FailingEntity_PersistsNothing()
        {
            using (TestDbContext seed = _database.CreateContext())
            {
                seed.Tags.Add(new Tag { Code = "dup", Label = "existing" });
                seed.SaveChanges();
            }
            WriteRepository<Tag> repository = new(_context);

            Assert.CatchAsync<DbUpdateException>(() => repository.InsertAsync(new List<Tag>
            {
                new() { Code = "new", Label = "fresh" },
                new() { Code = "dup", Label = "conflict" }
            }));

            using TestDbContext context = _database.CreateContext();
            Assert.That(context.Tags.Select(t => t.Code).ToList(), Is.EquivalentTo(new[] { "dup" }));
        }

        [Test]
        public void InsertAsync_List_NullElement_ThrowsAndPersistsNothing()
        {
            WriteRepository<Person> repository = new(_context);

            Assert.ThrowsAsync<ArgumentNullException>(() => repository.InsertAsync(new List<Person> { new() { Name = "Ada" }, null }));

            using TestDbContext context = _database.CreateContext();
            Assert.That(context.Persons.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task UpdateAsync_CompositeKey_FirstPartDefault_Persists()
        {
            WriteRepository<OrderLine> repository = new(_context);
            await repository.InsertAsync(new OrderLine { OrderId = 0, LineNumber = 1, Product = "keyboard" });
            repository.ClearChangeTracker();

            bool updated = await repository.UpdateAsync(new OrderLine { OrderId = 0, LineNumber = 1, Product = "trackball" });

            using TestDbContext context = _database.CreateContext();
            Assert.Multiple(() =>
            {
                Assert.That(updated, Is.True);
                Assert.That(context.OrderLines.Single().Product, Is.EqualTo("trackball"));
            });
        }

        [Test]
        public async Task UpdateAsync_CompositeKey_UntrackedEntity_Persists()
        {
            WriteRepository<OrderLine> repository = new(_context);
            await repository.InsertAsync(new List<OrderLine>
            {
                new() { OrderId = 1, LineNumber = 1, Product = "keyboard" },
                new() { OrderId = 1, LineNumber = 2, Product = "mouse" }
            });
            repository.ClearChangeTracker();

            bool updated = await repository.UpdateAsync(new OrderLine { OrderId = 1, LineNumber = 2, Product = "pen" });

            using TestDbContext context = _database.CreateContext();
            Assert.Multiple(() =>
            {
                Assert.That(updated, Is.True);
                Assert.That(context.OrderLines.Single(l => l.LineNumber == 2).Product, Is.EqualTo("pen"));
                Assert.That(context.OrderLines.Single(l => l.LineNumber == 1).Product, Is.EqualTo("keyboard"));
            });
        }

        [Test]
        public void UpdateAsync_CompositeKey_AllPartsDefault_Throws()
        {
            WriteRepository<OrderLine> repository = new(_context);

            Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(new OrderLine { Product = "orphan" }));
        }

        [Test]
        public async Task GetListAsync_PaginationWithoutOrderBy_OrdersByPrimaryKey()
        {
            SeedTags();
            ReadRepository<Tag> repository = new(_context);

            PaginatedList<Tag> first = await repository.GetListAsync(new PaginationSpecification<Tag> { PageIndex = 1, PageSize = 2 });
            PaginatedList<Tag> second = await repository.GetListAsync(new PaginationSpecification<Tag> { PageIndex = 2, PageSize = 2 });

            Assert.Multiple(() =>
            {
                Assert.That(first.Items.Select(t => t.Code), Is.EqualTo(new[] { "a", "b" }));
                Assert.That(second.Items.Select(t => t.Code), Is.EqualTo(new[] { "c" }));
                Assert.That(first.TotalItems, Is.EqualTo(3));
            });
        }

        [Test]
        public async Task GetListAsync_PaginationWithoutOrderBy_CompositeKey_OrdersByEveryKeyPart()
        {
            SeedOrderLines();
            ReadRepository<OrderLine> repository = new(_context);

            PaginatedList<OrderLine> page = await repository.GetListAsync(new PaginationSpecification<OrderLine> { PageIndex = 1, PageSize = 2 });

            Assert.That(page.Items.Select(l => l.Product), Is.EqualTo(new[] { "keyboard", "mouse" }));
        }

        [Test]
        public async Task GetListAsync_PaginationWithoutOrderBy_WithCondition_OrdersByPrimaryKey()
        {
            SeedTags();
            ReadRepository<Tag> repository = new(_context);
            PaginationSpecification<Tag> specification = new() { PageIndex = 1, PageSize = 1 };
            specification.Conditions.Add(t => t.Code != "b");

            PaginatedList<Tag> page = await repository.GetListAsync(specification);

            Assert.Multiple(() =>
            {
                Assert.That(page.Items.Select(t => t.Code), Is.EqualTo(new[] { "a" }));
                Assert.That(page.TotalItems, Is.EqualTo(2));
            });
        }

        [Test]
        public async Task GetListAsync_PaginationAndProjectionWithoutOrderBy_OrdersByPrimaryKey()
        {
            SeedTags();
            ReadRepository<Tag> repository = new(_context);

            PaginatedList<string> page = await repository.GetListAsync(new PaginationSpecification<Tag> { PageIndex = 1, PageSize = 2 }, t => t.Label);

            Assert.That(page.Items, Is.EqualTo(new[] { "gamma", "alpha" }));
        }

        [Test]
        public void Any_PaginationAndProjectionWithoutOrderBy_OrdersByPrimaryKey()
        {
            SeedTags();
            ReadRepository<Tag> repository = new(_context);

            repository.Any(out PaginatedList<string> page, new PaginationSpecification<Tag> { PageIndex = 1, PageSize = 2 }, t => t.Label);

            Assert.That(page.Items, Is.EqualTo(new[] { "gamma", "alpha" }));
        }

        [Test]
        public async Task GetListAsync_PaginationWithOrderBy_KeepsCallerOrder()
        {
            SeedTags();
            ReadRepository<Tag> repository = new(_context);
            PaginationSpecification<Tag> specification = new() { PageIndex = 1, PageSize = 3 };
            specification.OrderBy = q => q.OrderBy(t => t.Label);

            PaginatedList<Tag> page = await repository.GetListAsync(specification);
            PaginatedList<string> projected = await repository.GetListAsync(specification, t => t.Code);

            Assert.Multiple(() =>
            {
                Assert.That(page.Items.Select(t => t.Code), Is.EqualTo(new[] { "b", "c", "a" }));
                Assert.That(projected.Items, Is.EqualTo(new[] { "b", "c", "a" }));
            });
        }
    }
}
