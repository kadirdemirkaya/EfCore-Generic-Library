using EfCore.Repository.Abstractions;
using EfCore.Repository.Concretes;
using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class KeysetPaginationTests
    {
        private SqliteTestDatabase _database;

        [SetUp]
        public void SetUp() => _database = new SqliteTestDatabase();

        [TearDown]
        public void TearDown() => _database.Dispose();

        private void SeedNumbered(int count, int firstNumber = 1)
            => _database.Seed(Enumerable.Range(firstNumber, count).Select(i => new Person { Name = $"P{i:D3}", Age = i }).ToArray());

        private static async Task<List<KeysetPage<Person, int>>> WalkByIdAsync(IReadRepository<Person> repository, int pageSize, bool descending = false)
        {
            List<KeysetPage<Person, int>> pages = new();
            int? cursor = null;

            while (true)
            {
                KeysetPage<Person, int> page = await repository.GetPageAfterAsync(p => p.Id, cursor, pageSize, descending: descending);
                pages.Add(page);

                if (!page.HasNextPage)
                {
                    return pages;
                }

                cursor = page.NextCursor;
            }
        }

        [TestCase(1, 5, 12, false, true)]
        [TestCase(2, 5, 12, true, true)]
        [TestCase(3, 5, 12, true, false)]
        [TestCase(2, 5, 10, true, false)]
        [TestCase(1, 5, 5, false, false)]
        [TestCase(1, 5, 3, false, false)]
        [TestCase(1, 5, 0, false, false)]
        [TestCase(4, 5, 12, true, false)]
        public void PaginatedList_HasPreviousAndNextPage_FollowPageIndexAndTotalPages(int pageIndex, int pageSize, long totalItems, bool hasPrevious, bool hasNext)
        {
            PaginatedList<Person> page = new(new List<Person>(), totalItems, pageIndex, pageSize);

            Assert.Multiple(() =>
            {
                Assert.That(page.HasPreviousPage, Is.EqualTo(hasPrevious));
                Assert.That(page.HasNextPage, Is.EqualTo(hasNext));
            });
        }

        [Test]
        public async Task PaginatedList_FromRepository_ReportsNavigationForEveryPage()
        {
            _database.Seed(SqliteTestDatabase.SamplePersons());
            using TestDbContext context = _database.CreateContext();
            ReadRepository<Person> repository = new(context);

            PaginatedList<Person>[] pages =
            {
                await repository.GetListAsync(new PaginationSpecification<Person> { PageIndex = 1, PageSize = 2 }),
                await repository.GetListAsync(new PaginationSpecification<Person> { PageIndex = 2, PageSize = 2 }),
                await repository.GetListAsync(new PaginationSpecification<Person> { PageIndex = 3, PageSize = 2 })
            };

            Assert.Multiple(() =>
            {
                Assert.That(pages.Select(p => p.HasPreviousPage), Is.EqualTo(new[] { false, true, true }));
                Assert.That(pages.Select(p => p.HasNextPage), Is.EqualTo(new[] { true, true, false }));
                Assert.That(pages.Select(p => p.TotalPages), Is.All.EqualTo(3));
            });
        }

        [Test]
        public async Task PaginatedList_EmptyTable_HasNeitherPreviousNorNextPage()
        {
            using TestDbContext context = _database.CreateContext();
            PaginatedList<Person> page = await context.Persons.OrderBy(p => p.Id).ToPaginatedListAsync(1, 10);

            Assert.Multiple(() =>
            {
                Assert.That(page.TotalPages, Is.EqualTo(0));
                Assert.That(page.HasPreviousPage, Is.False);
                Assert.That(page.HasNextPage, Is.False);
            });
        }

        [Test]
        public async Task GetPageAfterAsync_Ascending_VisitsEveryRowOnceInKeyOrder()
        {
            SeedNumbered(23);
            using TestDbContext context = _database.CreateContext();
            List<int> expected = await context.Persons.OrderBy(p => p.Id).Select(p => p.Id).ToListAsync();

            List<KeysetPage<Person, int>> pages = await WalkByIdAsync(new ReadRepository<Person>(context), 5);

            Assert.Multiple(() =>
            {
                Assert.That(pages.SelectMany(p => p.Items).Select(p => p.Id), Is.EqualTo(expected));
                Assert.That(pages.Select(p => p.Items.Count), Is.EqualTo(new[] { 5, 5, 5, 5, 3 }));
                Assert.That(pages.Select(p => p.HasNextPage), Is.EqualTo(new[] { true, true, true, true, false }));
                Assert.That(pages.Select(p => p.NextCursor), Is.EqualTo(pages.Select(p => p.Items[^1].Id)));
                Assert.That(pages.Select(p => p.PageSize), Is.All.EqualTo(5));
            });
        }

        [Test]
        public async Task GetPageAfterAsync_RowCountMultipleOfPageSize_LastFullPageHasNoNextPage()
        {
            SeedNumbered(10);
            using TestDbContext context = _database.CreateContext();

            List<KeysetPage<Person, int>> pages = await WalkByIdAsync(new ReadRepository<Person>(context), 5);

            Assert.Multiple(() =>
            {
                Assert.That(pages, Has.Count.EqualTo(2));
                Assert.That(pages[1].Items, Has.Count.EqualTo(5));
                Assert.That(pages[1].HasNextPage, Is.False);
                Assert.That(pages.SelectMany(p => p.Items).Select(p => p.Id).Distinct().Count(), Is.EqualTo(10));
            });
        }

        [Test]
        public async Task GetPageAfterAsync_Descending_VisitsEveryRowOnceInReverseKeyOrder()
        {
            SeedNumbered(12);
            using TestDbContext context = _database.CreateContext();
            List<int> expected = await context.Persons.OrderByDescending(p => p.Id).Select(p => p.Id).ToListAsync();

            List<KeysetPage<Person, int>> pages = await WalkByIdAsync(new ReadRepository<Person>(context), 5, descending: true);

            Assert.Multiple(() =>
            {
                Assert.That(pages.SelectMany(p => p.Items).Select(p => p.Id), Is.EqualTo(expected));
                Assert.That(pages.Select(p => p.Items.Count), Is.EqualTo(new[] { 5, 5, 2 }));
                Assert.That(pages[^1].HasNextPage, Is.False);
            });
        }

        [Test]
        public async Task GetPageAfterAsync_RowsInsertedAndDeletedBeforeTheCursor_DoNotShiftLaterPages()
        {
            SeedNumbered(12);
            using TestDbContext context = _database.CreateContext();
            ReadRepository<Person> repository = new(context);
            List<int> original = await context.Persons.OrderByDescending(p => p.Id).Select(p => p.Id).ToListAsync();

            KeysetPage<Person, int> first = await repository.GetPageAfterAsync(p => p.Id, null, 5, descending: true);

            SeedNumbered(3, 100);
            using (TestDbContext writer = _database.CreateContext())
            {
                writer.Persons.Remove(await writer.Persons.SingleAsync(p => p.Id == first.Items[0].Id));
                await writer.SaveChangesAsync();
            }

            PaginatedList<Person> offsetPage = await context.Persons.AsNoTracking().OrderByDescending(p => p.Id).ToPaginatedListAsync(2, 5);
            KeysetPage<Person, int> second = await repository.GetPageAfterAsync(p => p.Id, first.NextCursor, 5, descending: true);
            KeysetPage<Person, int> third = await repository.GetPageAfterAsync(p => p.Id, second.NextCursor, 5, descending: true);

            Assert.Multiple(() =>
            {
                Assert.That(offsetPage.Items.Select(p => p.Id), Is.Not.EqualTo(original.Skip(5).Take(5)));
                Assert.That(second.Items.Select(p => p.Id), Is.EqualTo(original.Skip(5).Take(5)));
                Assert.That(third.Items.Select(p => p.Id), Is.EqualTo(original.Skip(10)));
                Assert.That(third.HasNextPage, Is.False);
            });
        }

        [Test]
        public async Task GetPageAfterAsync_StringKey_PagesByComparingTheKey()
        {
            _database.Seed(SqliteTestDatabase.SamplePersons());
            using TestDbContext context = _database.CreateContext();
            IReadRepository<Person> repository = new ReadRepository<Person>(context);

            KeysetPage<Person, string> first = await repository.GetPageAfterAsync(p => p.Name, null, 2);
            KeysetPage<Person, string> second = await repository.GetPageAfterAsync(p => p.Name, first.NextCursor, 2);
            KeysetPage<Person, string> third = await repository.GetPageAfterAsync(p => p.Name, second.NextCursor, 2);
            KeysetPage<Person, string> back = await repository.GetPageAfterAsync(p => p.Name, "Barbara", 10, descending: true);

            Assert.Multiple(() =>
            {
                Assert.That(first.Items.Select(p => p.Name), Is.EqualTo(new[] { "Ada", "Alan" }));
                Assert.That(first.NextCursor, Is.EqualTo("Alan"));
                Assert.That(second.Items.Select(p => p.Name), Is.EqualTo(new[] { "Barbara", "Grace" }));
                Assert.That(third.Items.Select(p => p.Name), Is.EqualTo(new[] { "Linus" }));
                Assert.That(third.HasNextPage, Is.False);
                Assert.That(back.Items.Select(p => p.Name), Is.EqualTo(new[] { "Alan", "Ada" }));
            });
        }

        [Test]
        public async Task GetPageAfterAsync_Filter_PagesOnlyMatchingRows()
        {
            SeedNumbered(20);
            using TestDbContext context = _database.CreateContext();
            IReadRepository<Person> repository = new UnitOfWork(context).GetReadRepository<Person>();

            KeysetPage<Person, int> first = await repository.GetPageAfterAsync(p => p.Id, null, 4, p => p.Age % 2 == 0, asNoTracking: true);
            KeysetPage<Person, int> second = await repository.GetPageAfterAsync(p => p.Id, first.NextCursor, 4, p => p.Age % 2 == 0, asNoTracking: true);
            KeysetPage<Person, int> third = await repository.GetPageAfterAsync(p => p.Id, second.NextCursor, 4, p => p.Age % 2 == 0, asNoTracking: true);

            Assert.Multiple(() =>
            {
                Assert.That(first.Items.Concat(second.Items).Concat(third.Items).Select(p => p.Age), Is.EqualTo(new[] { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 }));
                Assert.That(new[] { first.HasNextPage, second.HasNextPage, third.HasNextPage }, Is.EqualTo(new[] { true, true, false }));
                Assert.That(context.ChangeTracker.Entries().Count(), Is.EqualTo(0));
            });
        }

        [Test]
        public async Task GetPageAfterAsync_Specification_AppliesConditionsIncludesAndTracking()
        {
            _database.Seed(SqliteTestDatabase.SamplePersons());
            using TestDbContext context = _database.CreateContext();
            IReadRepository<Person> repository = new ReadRepository<Person>(context);
            SpecificationBase<Person> specification = new() { AsNoTracking = true, Includes = q => q.Include(p => p.Baskets) };
            specification.Conditions.Add(p => p.Age > 30);
            specification.Conditions.Add(p => p.Age < 80);

            KeysetPage<Person, int> first = await repository.GetPageAfterAsync(p => p.Age, null, 2, specification);
            KeysetPage<Person, int> second = await repository.GetPageAfterAsync(p => p.Age, first.NextCursor, 2, specification);

            Assert.Multiple(() =>
            {
                Assert.That(first.Items.Select(p => p.Name), Is.EqualTo(new[] { "Ada", "Alan" }));
                Assert.That(first.Items[0].Baskets.Select(b => b.Description).OrderBy(d => d), Is.EqualTo(new[] { "books", "tools" }));
                Assert.That(first.HasNextPage, Is.True);
                Assert.That(first.NextCursor, Is.EqualTo(41));
                Assert.That(second.Items.Select(p => p.Name), Is.EqualTo(new[] { "Barbara" }));
                Assert.That(second.HasNextPage, Is.False);
                Assert.That(context.ChangeTracker.Entries().Count(), Is.EqualTo(0));
            });
        }

        [Test]
        public async Task GetPageAfterAsync_SpecificationIgnoringQueryFilters_PagesHiddenRows()
        {
            await using SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            DbContextOptions<FilteredDbContext> options = new DbContextOptionsBuilder<FilteredDbContext>().UseSqlite(connection).Options;
            await using (FilteredDbContext seed = new(options))
            {
                await seed.Database.EnsureCreatedAsync();
                seed.Items.AddRange(
                    new ArchivableItem { Name = "a", Stock = 1 },
                    new ArchivableItem { Name = "b", Stock = 2, IsArchived = true },
                    new ArchivableItem { Name = "c", Stock = 3 });
                await seed.SaveChangesAsync();
            }

            await using FilteredDbContext context = new(options);
            IReadRepository<ArchivableItem> repository = new ReadRepository<ArchivableItem>(context);

            KeysetPage<ArchivableItem, int> filtered = await repository.GetPageAfterAsync(i => i.Stock, null, 10, new SpecificationBase<ArchivableItem>());
            KeysetPage<ArchivableItem, int> all = await repository.GetPageAfterAsync(i => i.Stock, null, 10, new SpecificationBase<ArchivableItem> { IgnoreQueryFilters = true });

            Assert.Multiple(() =>
            {
                Assert.That(filtered.Items.Select(i => i.Name), Is.EqualTo(new[] { "a", "c" }));
                Assert.That(all.Items.Select(i => i.Name), Is.EqualTo(new[] { "a", "b", "c" }));
            });
        }

        [Test]
        public async Task GetPageAfterAsync_RunsOneCommandPerPage()
        {
            SeedNumbered(7);
            CommandCounter counter = new();
            using TestDbContext context = _database.CreateContext(counter);
            IReadRepository<Person> repository = new ReadRepository<Person>(context);

            KeysetPage<Person, int> first = await repository.GetPageAfterAsync(p => p.Id, null, 5);
            int afterFirst = counter.Asynchronous;
            KeysetPage<Person, int> second = await repository.GetPageAfterAsync(p => p.Id, first.NextCursor, 5);

            Assert.Multiple(() =>
            {
                Assert.That(afterFirst, Is.EqualTo(1));
                Assert.That(counter.Asynchronous, Is.EqualTo(2));
                Assert.That(counter.Synchronous, Is.EqualTo(0));
                Assert.That(second.Items, Has.Count.EqualTo(2));
            });
        }

        [Test]
        public async Task GetPageAfterAsync_EmptyResult_HasNoItemsAndNoNextPage()
        {
            SeedNumbered(3);
            using TestDbContext context = _database.CreateContext();
            IReadRepository<Person> repository = new ReadRepository<Person>(context);

            KeysetPage<Person, int> afterLast = await repository.GetPageAfterAsync(p => p.Id, int.MaxValue - 1, 5);
            KeysetPage<Person, string> noMatch = await repository.GetPageAfterAsync(p => p.Name, null, 5, p => p.Age > 100);

            Assert.Multiple(() =>
            {
                Assert.That(afterLast.Items, Is.Empty);
                Assert.That(afterLast.HasNextPage, Is.False);
                Assert.That(afterLast.NextCursor, Is.EqualTo(0));
                Assert.That(noMatch.Items, Is.Empty);
                Assert.That(noMatch.HasNextPage, Is.False);
                Assert.That(noMatch.NextCursor, Is.Null);
            });
        }

        [Test]
        public void GetPageAfterAsync_InvalidArguments_Throw()
        {
            using TestDbContext context = _database.CreateContext();
            IReadRepository<Person> repository = new ReadRepository<Person>(context);
            Specification<Person> skipping = new() { Skip = 1 };
            Specification<Person> taking = new() { Take = 1 };
            SpecificationBase<Person> ordered = new() { OrderBy = q => q.OrderBy(p => p.Name) };
            SpecificationBase<Person> nullCondition = new();
            nullCondition.Conditions.Add(null);

            Assert.Multiple(() =>
            {
                Assert.ThrowsAsync<ArgumentNullException>(() => ((IReadRepository<Person>)null).GetPageAfterAsync(p => p.Id, null, 5));
                Assert.ThrowsAsync<ArgumentNullException>(() => repository.GetPageAfterAsync((System.Linq.Expressions.Expression<Func<Person, int>>)null, null, 5));
                Assert.ThrowsAsync<ArgumentNullException>(() => repository.GetPageAfterAsync(p => p.Name, null, 5, (SpecificationBase<Person>)null));
                Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.GetPageAfterAsync(p => p.Id, null, 0));
                Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.GetPageAfterAsync(p => p.Name, null, -1, new SpecificationBase<Person>()));
                Assert.ThrowsAsync<ArgumentException>(() => repository.GetPageAfterAsync(p => p.Id, null, 5, skipping));
                Assert.ThrowsAsync<ArgumentException>(() => repository.GetPageAfterAsync(p => p.Id, null, 5, taking));
                Assert.ThrowsAsync<ArgumentException>(() => repository.GetPageAfterAsync(p => p.Id, null, 5, ordered));
                Assert.ThrowsAsync<ArgumentException>(() => repository.GetPageAfterAsync(p => p.Id, null, 5, new PaginationSpecification<Person> { PageIndex = 1, PageSize = 5 }));
                Assert.ThrowsAsync<ArgumentException>(() => repository.GetPageAfterAsync(p => p.Id, null, 5, nullCondition));
            });
        }

        [Test]
        public void GetPageAfterAsync_CancelledToken_Throws()
        {
            SeedNumbered(3);
            using TestDbContext context = _database.CreateContext();
            IReadRepository<Person> repository = new ReadRepository<Person>(context);
            using CancellationTokenSource source = new();
            source.Cancel();

            Assert.CatchAsync<OperationCanceledException>(() => repository.GetPageAfterAsync(p => p.Id, null, 5, cancellationToken: source.Token));
        }

        [Test]
        public void KeysetPage_Constructor_CopiesTheItems()
        {
            List<Person> items = new() { new Person { Id = 1 }, new Person { Id = 2 } };
            KeysetPage<Person, int> page = new(items, 2, true, 2);
            items.Clear();

            Assert.Multiple(() =>
            {
                Assert.That(page.Items, Has.Count.EqualTo(2));
                Assert.That(page.PageSize, Is.EqualTo(2));
                Assert.That(page.HasNextPage, Is.True);
                Assert.That(page.NextCursor, Is.EqualTo(2));
                Assert.Throws<ArgumentNullException>(() => _ = new KeysetPage<Person, int>(null, 2, false, 0));
            });
        }
    }
}
