using EfCore.Repository.Abstractions;
using EfCore.Repository.Concretes;
using EfCore.Repository.Expressions;
using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class SpecificationCompositionTests
    {
        private SqliteTestDatabase _database;
        private TestDbContext _context;
        private ReadRepository<Person> _repository;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();
            _database.Seed(SqliteTestDatabase.SamplePersons());
            _context = _database.CreateContext();
            _repository = new ReadRepository<Person>(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
            _database.Dispose();
        }

        private static string[] Names(IEnumerable<Person> persons) => persons.Select(p => p.Name).ToArray();

        private static FilteredDbContext CreateFilteredContext(SqliteConnection connection)
        {
            connection.Open();
            DbContextOptions<FilteredDbContext> options = new DbContextOptionsBuilder<FilteredDbContext>().UseSqlite(connection).Options;

            using (FilteredDbContext seed = new(options))
            {
                seed.Database.EnsureCreated();
                seed.Items.AddRange(
                    new ArchivableItem { Name = "kept" },
                    new ArchivableItem { Name = "archived", IsArchived = true });
                seed.SaveChanges();
            }

            return new FilteredDbContext(options);
        }

        [Test]
        public async Task Builder_Specification_ProducesTheSameSqlAndRowsAsProperties()
        {
            Specification<Person> byProperties = new()
            {
                Conditions = { p => p.Age > 30 },
                Includes = q => q.Include(p => p.Baskets),
                OrderBy = q => q.OrderByDescending(p => p.Age).ThenBy(p => p.Name),
                AsNoTracking = true,
                Skip = 1,
                Take = 2
            };

            Specification<Person> byBuilder = new Specification<Person>()
                .Where(p => p.Age > 30)
                .Include(p => p.Baskets)
                .OrderByDescending(p => p.Age)
                .ThenBy(p => p.Name)
                .AsNoTracking()
                .Skip(1)
                .Take(2);

            string expectedSql = _context.Persons.GetSpecifiedQuery(byProperties).ToQueryString();
            string actualSql = _context.Persons.GetSpecifiedQuery(byBuilder).ToQueryString();

            List<Person> expected = await _repository.GetListAsync(byProperties);
            List<Person> actual = await _repository.GetListAsync(byBuilder);

            Assert.Multiple(() =>
            {
                Assert.That(actualSql, Is.EqualTo(expectedSql));
                Assert.That(Names(actual), Is.EqualTo(Names(expected)));
                Assert.That(Names(actual), Is.EqualTo(new[] { "Barbara", "Alan" }));
                Assert.That(actual.Single(p => p.Name == "Alan").Baskets, Has.Count.EqualTo(1));
                Assert.That(byBuilder.Skip, Is.EqualTo(1));
                Assert.That(byBuilder.Take, Is.EqualTo(2));
                Assert.That(byBuilder.AsNoTracking, Is.True);
                Assert.That(_context.ChangeTracker.Entries().Count(), Is.EqualTo(0));
            });
        }

        [Test]
        public async Task Builder_PaginationSpecification_ProducesTheSameSqlAndPageAsProperties()
        {
            PaginationSpecification<Person> byProperties = new()
            {
                Conditions = { p => p.Age > 30 },
                OrderBy = q => q.OrderBy(p => p.Name),
                AsNoTracking = true,
                PageIndex = 2,
                PageSize = 2
            };

            PaginationSpecification<Person> byBuilder = new PaginationSpecification<Person>()
                .Where(p => p.Age > 30)
                .OrderByAscending(p => p.Name)
                .AsNoTracking()
                .Page(2, 2);

            string expectedSql = _context.Persons.GetSpecifiedQuery(byProperties).ToQueryString();
            string actualSql = _context.Persons.GetSpecifiedQuery(byBuilder).ToQueryString();

            PaginatedList<Person> expected = await _repository.GetListAsync(byProperties);
            PaginatedList<Person> actual = await _repository.GetListAsync(byBuilder);

            Assert.Multiple(() =>
            {
                Assert.That(actualSql, Is.EqualTo(expectedSql));
                Assert.That(Names(actual.Items), Is.EqualTo(Names(expected.Items)));
                Assert.That(Names(actual.Items), Is.EqualTo(new[] { "Barbara", "Grace" }));
                Assert.That(actual.TotalItems, Is.EqualTo(expected.TotalItems));
                Assert.That(actual.TotalItems, Is.EqualTo(4));
                Assert.That(actual.PageIndex, Is.EqualTo(2));
                Assert.That(actual.PageSize, Is.EqualTo(2));
            });
        }

        [Test]
        public void Builder_QueryOptions_SetTheSameFlagsAsProperties()
        {
            Specification<Person> specification = new Specification<Person>()
                .AsNoTrackingWithIdentityResolution()
                .AsSplitQuery()
                .IgnoreQueryFilters();

            PaginationSpecification<Person> pagination = new PaginationSpecification<Person>()
                .AsNoTrackingWithIdentityResolution()
                .AsSplitQuery()
                .IgnoreQueryFilters();

            Assert.Multiple(() =>
            {
                Assert.That(specification.AsNoTrackingWithIdentityResolution, Is.True);
                Assert.That(specification.AsSplitQuery, Is.True);
                Assert.That(specification.IgnoreQueryFilters, Is.True);
                Assert.That(specification.AsNoTracking, Is.False);
                Assert.That(pagination.AsNoTrackingWithIdentityResolution, Is.True);
                Assert.That(pagination.AsSplitQuery, Is.True);
                Assert.That(pagination.IgnoreQueryFilters, Is.True);
                Assert.That(pagination.AsNoTracking, Is.False);
            });
        }

        [Test]
        public async Task Builder_IgnoreQueryFilters_ReturnsRowsTheFilterHides()
        {
            using SqliteConnection connection = new("Data Source=:memory:");
            using FilteredDbContext context = CreateFilteredContext(connection);
            ReadRepository<ArchivableItem> repository = new(context);

            List<ArchivableItem> filtered = await repository.GetListAsync(new Specification<ArchivableItem>().OrderByAscending(i => i.Name));
            List<ArchivableItem> all = await repository.GetListAsync(new Specification<ArchivableItem>().IgnoreQueryFilters().OrderByAscending(i => i.Name));

            Assert.Multiple(() =>
            {
                Assert.That(filtered.Select(i => i.Name), Is.EqualTo(new[] { "kept" }));
                Assert.That(all.Select(i => i.Name), Is.EqualTo(new[] { "archived", "kept" }));
            });
        }

        [Test]
        public void Builder_ReturnsTheSameInstanceAndKeepsPropertiesSetBefore()
        {
            Expression<Func<Person, bool>> existing = p => p.Age > 0;
            Specification<Person> specification = new() { Take = 3, Conditions = { existing } };
            PaginationSpecification<Person> pagination = new() { PageSize = 7 };

            Specification<Person> returned = specification.Where(p => p.Name != null).AsNoTracking();
            PaginationSpecification<Person> returnedPage = pagination.Where(p => p.Name != null).OrderByAscending(p => p.Id);

            Assert.Multiple(() =>
            {
                Assert.That(returned, Is.SameAs(specification));
                Assert.That(returnedPage, Is.SameAs(pagination));
                Assert.That(specification.Take, Is.EqualTo(3));
                Assert.That(specification.Conditions, Has.Count.EqualTo(2));
                Assert.That(specification.Conditions[0], Is.SameAs(existing));
                Assert.That(pagination.PageSize, Is.EqualTo(7));
                Assert.That(pagination.OrderBy, Is.Not.Null);
            });
        }

        [Test]
        public async Task Builder_WhereCalledTwice_RequiresBothConditions()
        {
            List<Person> persons = await _repository.GetListAsync(new Specification<Person>()
                .Where(p => p.Age > 30)
                .Where(p => p.Age < 60)
                .OrderByAscending(p => p.Name));

            Assert.That(Names(persons), Is.EqualTo(new[] { "Ada", "Alan", "Barbara" }));
        }

        [Test]
        public async Task Builder_WhereWithNullConditionsList_CreatesTheList()
        {
            Specification<Person> specification = new() { Conditions = null };

            specification.Where(p => p.Age > 80);

            Assert.That(Names(await _repository.GetListAsync(specification)), Is.EqualTo(new[] { "Grace" }));
        }

        [Test]
        public async Task Builder_Include_AppliesAfterIncludesAlreadySet()
        {
            using TestDbContext context = _database.CreateContext();
            ReadRepository<Basket> repository = new(context);
            List<string> calls = new();
            Specification<Basket> specification = new()
            {
                Includes = q =>
                {
                    calls.Add("property");
                    return q.Include(b => b.Person);
                }
            };

            specification
                .Include(q =>
                {
                    calls.Add("builder");
                    return q.Include(b => b.Person);
                })
                .Where(b => b.Description == "games")
                .AsNoTracking();

            List<Basket> baskets = await repository.GetListAsync(specification);

            Assert.Multiple(() =>
            {
                Assert.That(calls, Is.EqualTo(new[] { "property", "builder" }));
                Assert.That(baskets.Single().Person.Name, Is.EqualTo("Alan"));
            });
        }

        [Test]
        public void Builder_IncludeChain_ProducesTheSameSqlAsTheIncludesProperty()
        {
            Specification<Person> byProperties = new()
            {
                Includes = q => q.Include(p => p.Baskets)
            };

            Specification<Person> byBuilder = new Specification<Person>()
                .Include(q => q.Include(p => p.Baskets));

            Specification<Person> byNavigation = new Specification<Person>()
                .Include(p => p.Baskets);

            string expected = _context.Persons.GetSpecifiedQuery(byProperties).ToQueryString();

            Assert.Multiple(() =>
            {
                Assert.That(_context.Persons.GetSpecifiedQuery(byBuilder).ToQueryString(), Is.EqualTo(expected));
                Assert.That(_context.Persons.GetSpecifiedQuery(byNavigation).ToQueryString(), Is.EqualTo(expected));
            });
        }

        [Test]
        public async Task Builder_OrderByAscending_ReplacesAnExistingOrder()
        {
            Specification<Person> specification = new() { OrderBy = q => q.OrderByDescending(p => p.Age) };

            specification.OrderByAscending(p => p.Name);

            Assert.That(Names(await _repository.GetListAsync(specification)), Is.EqualTo(new[] { "Ada", "Alan", "Barbara", "Grace", "Linus" }));
        }

        [Test]
        public async Task Builder_ThenByDescending_AddsASecondKeyAfterTheOrderProperty()
        {
            _database.Seed(new Person { Name = "Zed", Age = 36 });
            Specification<Person> specification = new() { OrderBy = q => q.OrderBy(p => p.Age) };

            specification.ThenByDescending(p => p.Name).Take(3);

            Assert.That(Names(await _repository.GetListAsync(specification)), Is.EqualTo(new[] { "Linus", "Zed", "Ada" }));
        }

        [Test]
        public async Task Builder_PaginationThenBy_OrdersPageByBothKeys()
        {
            _database.Seed(new Person { Name = "Aaron", Age = 41 });

            PaginatedList<Person> page = await _repository.GetListAsync(new PaginationSpecification<Person>()
                .OrderByDescending(p => p.Age)
                .ThenBy(p => p.Name)
                .Page(1, 4));

            Assert.That(Names(page.Items), Is.EqualTo(new[] { "Grace", "Barbara", "Aaron", "Alan" }));
        }

        [Test]
        public void Builder_InvalidArguments_Throw()
        {
            Specification<Person> specification = new();
            PaginationSpecification<Person> pagination = new();
            Specification<Person> nullSpecification = null;
            PaginationSpecification<Person> nullPagination = null;

            Assert.Multiple(() =>
            {
                Assert.Throws<InvalidOperationException>(() => specification.ThenBy(p => p.Name));
                Assert.Throws<InvalidOperationException>(() => specification.ThenByDescending(p => p.Name));
                Assert.Throws<InvalidOperationException>(() => pagination.ThenBy(p => p.Name));
                Assert.Throws<InvalidOperationException>(() => pagination.ThenByDescending(p => p.Name));
                Assert.Throws<ArgumentOutOfRangeException>(() => specification.Skip(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => specification.Take(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => pagination.Page(0, 10));
                Assert.Throws<ArgumentOutOfRangeException>(() => pagination.Page(1, 0));
                Assert.Throws<ArgumentNullException>(() => specification.Where(null));
                Assert.Throws<ArgumentNullException>(() => pagination.Where(null));
                Assert.Throws<ArgumentNullException>(() => specification.Include((Expression<Func<Person, List<Basket>>>)null));
                Assert.Throws<ArgumentNullException>(() => specification.Include((Func<IQueryable<Person>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Person, object>>)null));
                Assert.Throws<ArgumentNullException>(() => specification.OrderByAscending((Expression<Func<Person, string>>)null));
                Assert.Throws<ArgumentNullException>(() => pagination.OrderByDescending((Expression<Func<Person, string>>)null));
                Assert.Throws<ArgumentNullException>(() => nullSpecification.Where(p => true));
                Assert.Throws<ArgumentNullException>(() => nullSpecification.AsNoTracking());
                Assert.Throws<ArgumentNullException>(() => nullSpecification.Skip(-1));
                Assert.Throws<ArgumentNullException>(() => nullSpecification.Take(1));
                Assert.Throws<ArgumentNullException>(() => nullSpecification.Include(p => p.Baskets));
                Assert.Throws<ArgumentNullException>(() => nullSpecification.OrderByAscending(p => p.Name));
                Assert.Throws<ArgumentNullException>(() => nullPagination.Page(0, 0));
                Assert.Throws<ArgumentNullException>(() => nullPagination.IgnoreQueryFilters());
                Assert.That(specification.Skip, Is.Null);
                Assert.That(specification.Take, Is.Null);
                Assert.That(pagination.PageIndex, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task Or_IsTranslatedAndReturnsRowsMatchingEitherCondition()
        {
            Expression<Func<Person, bool>> young = p => p.Age < 30;
            Expression<Func<Person, bool>> old = x => x.Age > 80;

            List<Person> persons = await _repository.GetListAsync(new Specification<Person>()
                .Where(young.Or(old))
                .OrderByAscending(p => p.Name));

            Assert.That(Names(persons), Is.EqualTo(new[] { "Grace", "Linus" }));
        }

        [Test]
        public async Task And_IsTranslatedAndReturnsRowsMatchingBothConditions()
        {
            Expression<Func<Person, bool>> startsWithA = p => p.Name.StartsWith("A");
            Expression<Func<Person, bool>> over40 = other => other.Age > 40;

            List<Person> persons = await _repository.GetListAsync(new Specification<Person>().Where(startsWithA.And(over40)));

            Assert.That(Names(persons), Is.EqualTo(new[] { "Alan" }));
        }

        [Test]
        public async Task Not_IsTranslatedAndReturnsRowsNotMatchingTheCondition()
        {
            Expression<Func<Person, bool>> startsWithA = p => p.Name.StartsWith("A");

            List<Person> persons = await _repository.GetListAsync(new Specification<Person>()
                .Where(startsWithA.Not())
                .OrderByAscending(p => p.Name));

            Assert.That(Names(persons), Is.EqualTo(new[] { "Barbara", "Grace", "Linus" }));
        }

        [Test]
        public async Task Combinators_NestedWithCapturedValues_AreTranslatedWithParameters()
        {
            int minimumAge = 40;
            string excluded = "Grace";
            Expression<Func<Person, bool>> young = p => p.Age < 30;
            Expression<Func<Person, bool>> older = p => p.Age > minimumAge;
            Expression<Func<Person, bool>> isExcluded = q => q.Name == excluded;

            Expression<Func<Person, bool>> filter = young.Or(older).And(isExcluded.Not());
            IQueryable<Person> query = _context.Persons.Where(filter).OrderBy(p => p.Name);

            string sql = query.ToQueryString();
            string commandText = sql.Substring(sql.IndexOf("SELECT", StringComparison.Ordinal));
            List<Person> persons = await query.ToListAsync();

            Assert.Multiple(() =>
            {
                Assert.That(Names(persons), Is.EqualTo(new[] { "Alan", "Barbara", "Linus" }));
                Assert.That(commandText, Does.Contain(" OR "));
                Assert.That(commandText, Does.Contain(" AND "));
                Assert.That(commandText, Does.Contain("@"));
                Assert.That(commandText, Does.Not.Contain("'Grace'"));
            });
        }

        [Test]
        public async Task Combinators_WorkAsRepositoryFilters()
        {
            Expression<Func<Person, bool>> young = p => p.Age < 30;
            Expression<Func<Person, bool>> old = p => p.Age > 80;

            List<Person> persons = await _repository.GetListAsync(young.Or(old));
            int count = await _repository.CountAsync(young.Or(old).Not());
            long longCount = await _repository.LongCountAsync(young.Or(old));

            Assert.Multiple(() =>
            {
                Assert.That(Names(persons).OrderBy(n => n), Is.EqualTo(new[] { "Grace", "Linus" }));
                Assert.That(count, Is.EqualTo(3));
                Assert.That(longCount, Is.EqualTo(2L));
            });
        }

        [Test]
        public void Combinators_DoNotChangeTheirArguments()
        {
            Expression<Func<Person, bool>> left = p => p.Age < 30;
            Expression<Func<Person, bool>> right = x => x.Age > 80;
            string leftText = left.ToString();
            string rightText = right.ToString();

            Expression<Func<Person, bool>> combined = left.Or(right);
            Expression<Func<Person, bool>> negated = left.Not();

            Assert.Multiple(() =>
            {
                Assert.That(left.ToString(), Is.EqualTo(leftText));
                Assert.That(right.ToString(), Is.EqualTo(rightText));
                Assert.That(combined, Is.Not.SameAs(left));
                Assert.That(negated, Is.Not.SameAs(left));
                Assert.That(combined.Parameters, Has.Count.EqualTo(1));
                Assert.That(combined.Compile()(new Person { Age = 90 }), Is.True);
                Assert.That(combined.Compile()(new Person { Age = 50 }), Is.False);
                Assert.That(left.And(right).Compile()(new Person { Age = 90 }), Is.False);
                Assert.That(negated.Compile()(new Person { Age = 20 }), Is.False);
            });
        }

        [Test]
        public void Combinators_NullArguments_Throw()
        {
            Expression<Func<Person, bool>> condition = p => p.Age > 1;
            Expression<Func<Person, bool>> missing = null;

            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentNullException>(() => condition.And(missing));
                Assert.Throws<ArgumentNullException>(() => missing.And(condition));
                Assert.Throws<ArgumentNullException>(() => condition.Or(missing));
                Assert.Throws<ArgumentNullException>(() => missing.Or(condition));
                Assert.Throws<ArgumentNullException>(() => missing.Not());
            });
        }

        [Test]
        public async Task LongCountAsync_CountsAllOrFilteredRows()
        {
            long all = await _repository.LongCountAsync();
            long filtered = await _repository.LongCountAsync(p => p.Age > 40);

            Assert.Multiple(() =>
            {
                Assert.That(all, Is.EqualTo(5L));
                Assert.That(filtered, Is.EqualTo(3L));
                Assert.That(filtered, Is.EqualTo((long)_context.Persons.Count(p => p.Age > 40)));
            });
        }

        [Test]
        public async Task LongCountAsync_ThroughUnitOfWorkRepository_RunsOneCountCommand()
        {
            CommandCounter counter = new();
            using TestDbContext context = _database.CreateContext(counter);
            IReadRepository<Person> repository = new UnitOfWork(context).GetReadRepository<Person>();

            long count = await repository.LongCountAsync(p => p.Name.StartsWith("A"));

            Assert.Multiple(() =>
            {
                Assert.That(count, Is.EqualTo(2L));
                Assert.That(counter.Asynchronous, Is.EqualTo(1));
                Assert.That(context.ChangeTracker.Entries().Count(), Is.EqualTo(0));
            });
        }

        [Test]
        public async Task LongCountAsync_AppliesGlobalQueryFilters()
        {
            using SqliteConnection connection = new("Data Source=:memory:");
            using FilteredDbContext context = CreateFilteredContext(connection);
            ReadRepository<ArchivableItem> repository = new(context);

            Assert.That(await repository.LongCountAsync(), Is.EqualTo(1L));
        }

        [Test]
        public void LongCountAsync_InvalidArgumentsOrCancelledToken_Throw()
        {
            IReadRepository<Person> missing = null;
            using CancellationTokenSource cancelled = new();
            cancelled.Cancel();

            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentNullException>(() => missing.LongCountAsync());
                Assert.ThrowsAsync(Is.InstanceOf<OperationCanceledException>(), async () => await _repository.LongCountAsync(cancellationToken: cancelled.Token));
            });
        }
    }
}
