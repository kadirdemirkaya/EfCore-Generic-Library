using EfCore.Repository.Abstractions;
using EfCore.Repository.Concretes;
using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class QueryOptionsAndStreamingTests
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

        private static async Task<(SqliteConnection Connection, DbContextOptions<FilteredDbContext> Options)> CreateFilteredDatabaseAsync()
        {
            SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync();
            DbContextOptions<FilteredDbContext> options = new DbContextOptionsBuilder<FilteredDbContext>().UseSqlite(connection).Options;
            await using FilteredDbContext seed = new(options);
            await seed.Database.EnsureCreatedAsync();
            seed.Items.AddRange(
                new ArchivableItem { Name = "active-1", Stock = 1 },
                new ArchivableItem { Name = "archived", Stock = 2, IsArchived = true },
                new ArchivableItem { Name = "active-2", Stock = 3 });
            await seed.SaveChangesAsync();
            return (connection, options);
        }

        private static string Shape(IEnumerable<Person> persons)
            => string.Join(";", persons.Select(p => $"{p.Name}:{string.Join(",", p.Baskets.Select(b => b.Description).OrderBy(d => d))}"));

        [Test]
        public void Specification_NewOptions_DefaultToOff()
        {
            SpecificationBase<Person> specification = new();

            Assert.Multiple(() =>
            {
                Assert.That(specification.AsSplitQuery, Is.False);
                Assert.That(specification.IgnoreQueryFilters, Is.False);
                Assert.That(specification.AsNoTrackingWithIdentityResolution, Is.False);
            });
        }

        [Test]
        public void GetSpecifiedQuery_DefaultSpecification_AddsNoQueryOption()
        {
            using TestDbContext context = _database.CreateContext();
            string expression = context.Persons.GetSpecifiedQuery(new Specification<Person>()).Expression.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(expression, Does.Not.Contain("AsSplitQuery"));
                Assert.That(expression, Does.Not.Contain("IgnoreQueryFilters"));
                Assert.That(expression, Does.Not.Contain("AsNoTracking"));
            });
        }

        [Test]
        public void GetSpecifiedQuery_EveryOverload_AppliesTheQueryOptions()
        {
            using TestDbContext context = _database.CreateContext();
            Specification<Person> specification = new() { AsSplitQuery = true, IgnoreQueryFilters = true, AsNoTrackingWithIdentityResolution = true };
            PaginationSpecification<Person> page = new() { PageIndex = 1, PageSize = 2, AsSplitQuery = true, IgnoreQueryFilters = true, AsNoTrackingWithIdentityResolution = true };

            string[] expressions =
            {
                context.Persons.GetSpecifiedQuery(specification).Expression.ToString(),
                context.Persons.GetSpecifiedQuery((SpecificationBase<Person>)specification).Expression.ToString(),
                context.Persons.GetSpecifiedQuery(page).Expression.ToString()
            };

            Assert.Multiple(() =>
            {
                foreach (string expression in expressions)
                {
                    Assert.That(expression, Does.Contain("AsSplitQuery()"));
                    Assert.That(expression, Does.Contain("IgnoreQueryFilters()"));
                    Assert.That(expression, Does.Contain("AsNoTrackingWithIdentityResolution()"));
                }
            });
        }

        [Test]
        public async Task AsSplitQuery_LoadsTheSameIncludesWithOneCommandPerCollection()
        {
            CommandCounter counter = new();
            await using TestDbContext context = _database.CreateContext(counter);
            ReadRepository<Person> repository = new(context);
            Specification<Person> single = new() { AsNoTracking = true, Includes = q => q.Include(p => p.Baskets), OrderBy = q => q.OrderBy(p => p.Id) };
            Specification<Person> split = new() { AsNoTracking = true, AsSplitQuery = true, Includes = q => q.Include(p => p.Baskets), OrderBy = q => q.OrderBy(p => p.Id) };

            List<Person> singleResult = await repository.GetListAsync(single);
            int singleCommands = counter.Asynchronous;
            counter.Reset();
            List<Person> splitResult = await repository.GetListAsync(split);
            int splitCommands = counter.Asynchronous;

            Assert.Multiple(() =>
            {
                Assert.That(singleCommands, Is.EqualTo(1));
                Assert.That(splitCommands, Is.EqualTo(2));
                Assert.That(Shape(splitResult), Is.EqualTo(Shape(singleResult)));
                Assert.That(Shape(splitResult), Is.EqualTo("Ada:books,tools;Alan:games;Grace:;Linus:;Barbara:"));
            });
        }

        [Test]
        public async Task AsSplitQuery_WithPagination_KeepsThePageAndItsIncludes()
        {
            CommandCounter counter = new();
            await using TestDbContext context = _database.CreateContext(counter);
            ReadRepository<Person> repository = new(context);
            PaginationSpecification<Person> page = new() { PageIndex = 1, PageSize = 2, AsSplitQuery = true, Includes = q => q.Include(p => p.Baskets) };

            PaginatedList<Person> result = await repository.GetListAsync(page);

            Assert.Multiple(() =>
            {
                Assert.That(result.TotalItems, Is.EqualTo(5));
                Assert.That(Shape(result.Items), Is.EqualTo("Ada:books,tools;Alan:games"));
                Assert.That(counter.Asynchronous, Is.EqualTo(3));
            });
        }

        [Test]
        public async Task IgnoreQueryFilters_ReturnsRowsHiddenByGlobalFilters()
        {
            (SqliteConnection connection, DbContextOptions<FilteredDbContext> options) = await CreateFilteredDatabaseAsync();
            await using (connection)
            {
                await using FilteredDbContext context = new(options);
                ReadRepository<ArchivableItem> repository = new(context);
                Specification<ArchivableItem> filtered = new() { OrderBy = q => q.OrderBy(i => i.Id) };
                Specification<ArchivableItem> unfiltered = new() { IgnoreQueryFilters = true, OrderBy = q => q.OrderBy(i => i.Id) };
                unfiltered.Conditions.Add(i => i.Stock > 1);

                List<ArchivableItem> visible = await repository.GetListAsync(filtered);
                List<ArchivableItem> all = await repository.GetListAsync(unfiltered);

                Assert.Multiple(() =>
                {
                    Assert.That(visible.Select(i => i.Name), Is.EqualTo(new[] { "active-1", "active-2" }));
                    Assert.That(all.Select(i => i.Name), Is.EqualTo(new[] { "archived", "active-2" }));
                });
            }
        }

        [Test]
        public async Task IgnoreQueryFilters_PaginatedReads_CountTheHiddenRowsToo()
        {
            (SqliteConnection connection, DbContextOptions<FilteredDbContext> options) = await CreateFilteredDatabaseAsync();
            await using (connection)
            {
                await using FilteredDbContext context = new(options);
                ReadRepository<ArchivableItem> repository = new(context);
                PaginationSpecification<ArchivableItem> page = new() { PageIndex = 1, PageSize = 2, IgnoreQueryFilters = true };
                PaginationSpecification<ArchivableItem> filteredPage = new() { PageIndex = 1, PageSize = 2 };

                PaginatedList<ArchivableItem> repositoryPage = await repository.GetListAsync(page);
                PaginatedList<string> mappedPage = await repository.GetListAsync(page, i => i.Name);
                PaginatedList<ArchivableItem> queryablePage = await context.Items.OrderBy(i => i.Id).ToPaginatedListAsync(page);
                PaginatedList<ArchivableItem> filtered = await repository.GetListAsync(filteredPage);

                Assert.Multiple(() =>
                {
                    Assert.That(repositoryPage.TotalItems, Is.EqualTo(3));
                    Assert.That(repositoryPage.Items.Select(i => i.Name), Is.EqualTo(new[] { "active-1", "archived" }));
                    Assert.That(mappedPage.TotalItems, Is.EqualTo(3));
                    Assert.That(mappedPage.Items, Is.EqualTo(new[] { "active-1", "archived" }));
                    Assert.That(queryablePage.TotalItems, Is.EqualTo(3));
                    Assert.That(queryablePage.Items.Select(i => i.Name), Is.EqualTo(new[] { "active-1", "archived" }));
                    Assert.That(filtered.TotalItems, Is.EqualTo(2));
                    Assert.That(filtered.Items.Select(i => i.Name), Is.EqualTo(new[] { "active-1", "active-2" }));
                });
            }
        }

        [Test]
        public async Task AsNoTrackingWithIdentityResolution_SharesInstancesWithoutTracking()
        {
            await using TestDbContext context = _database.CreateContext();
            ReadRepository<Basket> repository = new(context);
            Specification<Basket> plain = new() { AsNoTracking = true, Includes = q => q.Include(b => b.Person), OrderBy = q => q.OrderBy(b => b.Id) };
            plain.Conditions.Add(b => b.Person.Name == "Ada");
            Specification<Basket> resolved = new() { AsNoTrackingWithIdentityResolution = true, Includes = q => q.Include(b => b.Person), OrderBy = q => q.OrderBy(b => b.Id) };
            resolved.Conditions.Add(b => b.Person.Name == "Ada");
            Specification<Basket> both = new() { AsNoTracking = true, AsNoTrackingWithIdentityResolution = true, Includes = q => q.Include(b => b.Person), OrderBy = q => q.OrderBy(b => b.Id) };
            both.Conditions.Add(b => b.Person.Name == "Ada");

            List<Basket> plainResult = await repository.GetListAsync(plain);
            List<Basket> resolvedResult = await repository.GetListAsync(resolved);
            List<Basket> bothResult = await repository.GetListAsync(both);

            Assert.Multiple(() =>
            {
                Assert.That(plainResult, Has.Count.EqualTo(2));
                Assert.That(ReferenceEquals(plainResult[0].Person, plainResult[1].Person), Is.False);
                Assert.That(resolvedResult, Has.Count.EqualTo(2));
                Assert.That(ReferenceEquals(resolvedResult[0].Person, resolvedResult[1].Person), Is.True);
                Assert.That(ReferenceEquals(bothResult[0].Person, bothResult[1].Person), Is.True);
                Assert.That(context.ChangeTracker.Entries().Count(), Is.EqualTo(0));
            });
        }

        [Test]
        public async Task AsNoTrackingWithIdentityResolution_WithPagination_IsKept()
        {
            await using TestDbContext context = _database.CreateContext();
            ReadRepository<Basket> repository = new(context);
            PaginationSpecification<Basket> page = new()
            {
                PageIndex = 1,
                PageSize = 2,
                AsNoTracking = true,
                AsNoTrackingWithIdentityResolution = true,
                Includes = q => q.Include(b => b.Person)
            };

            PaginatedList<Basket> result = await repository.GetListAsync(page);

            Assert.Multiple(() =>
            {
                Assert.That(result.Items.Select(b => b.Description), Is.EqualTo(new[] { "books", "tools" }));
                Assert.That(ReferenceEquals(result.Items[0].Person, result.Items[1].Person), Is.True);
                Assert.That(context.ChangeTracker.Entries().Count(), Is.EqualTo(0));
            });
        }

        [Test]
        public async Task StreamAsync_Specification_ReturnsEveryMatchingRowInOrder()
        {
            await using TestDbContext context = _database.CreateContext();
            IReadRepository<Person> repository = new ReadRepository<Person>(context);
            Specification<Person> specification = new() { AsNoTracking = true, OrderBy = q => q.OrderByDescending(p => p.Age) };
            specification.Conditions.Add(p => p.Age > 30);

            List<string> names = new();
            await foreach (Person person in repository.StreamAsync(specification))
            {
                names.Add(person.Name);
            }

            Assert.Multiple(() =>
            {
                Assert.That(names, Is.EqualTo(new[] { "Grace", "Barbara", "Alan", "Ada" }));
                Assert.That(context.ChangeTracker.Entries().Count(), Is.EqualTo(0));
            });
        }

        [Test]
        public async Task StreamAsync_Specification_AppliesSkipTakeIncludesAndSplitQuery()
        {
            await using TestDbContext context = _database.CreateContext();
            IReadRepository<Person> repository = new ReadRepository<Person>(context);
            Specification<Person> specification = new()
            {
                Skip = 1,
                Take = 3,
                AsSplitQuery = true,
                Includes = q => q.Include(p => p.Baskets),
                OrderBy = q => q.OrderBy(p => p.Id)
            };

            List<Person> persons = new();
            await foreach (Person person in repository.StreamAsync(specification))
            {
                persons.Add(person);
            }

            Assert.That(Shape(persons), Is.EqualTo("Alan:games;Grace:;Linus:"));
        }

        [Test]
        public async Task StreamAsync_Filter_ReturnsMatchingRowsAndHonoursTracking()
        {
            await using TestDbContext context = _database.CreateContext();
            IReadRepository<Person> repository = new UnitOfWork(context).GetReadRepository<Person>();

            List<string> untracked = new();
            await foreach (Person person in repository.StreamAsync(p => p.Age < 50, asNoTracking: true))
            {
                untracked.Add(person.Name);
            }

            int trackedAfterNoTracking = context.ChangeTracker.Entries().Count();

            List<string> tracked = new();
            await foreach (Person person in repository.StreamAsync(p => p.Age < 50))
            {
                tracked.Add(person.Name);
            }

            Assert.Multiple(() =>
            {
                Assert.That(untracked, Is.EquivalentTo(new[] { "Ada", "Alan", "Linus" }));
                Assert.That(trackedAfterNoTracking, Is.EqualTo(0));
                Assert.That(tracked, Is.EquivalentTo(new[] { "Ada", "Alan", "Linus" }));
                Assert.That(context.ChangeTracker.Entries<Person>().Count(), Is.EqualTo(3));
            });
        }

        [Test]
        public async Task StreamAsync_IgnoreQueryFilters_StreamsHiddenRows()
        {
            (SqliteConnection connection, DbContextOptions<FilteredDbContext> options) = await CreateFilteredDatabaseAsync();
            await using (connection)
            {
                await using FilteredDbContext context = new(options);
                IReadRepository<ArchivableItem> repository = new ReadRepository<ArchivableItem>(context);

                List<string> all = new();
                await foreach (ArchivableItem item in repository.StreamAsync(new Specification<ArchivableItem> { IgnoreQueryFilters = true, OrderBy = q => q.OrderBy(i => i.Id) }))
                {
                    all.Add(item.Name);
                }

                List<string> visible = new();
                await foreach (ArchivableItem item in repository.StreamAsync(i => i.Stock > 0))
                {
                    visible.Add(item.Name);
                }

                Assert.Multiple(() =>
                {
                    Assert.That(all, Is.EqualTo(new[] { "active-1", "archived", "active-2" }));
                    Assert.That(visible, Is.EquivalentTo(new[] { "active-1", "active-2" }));
                });
            }
        }

        [Test]
        public async Task StreamAsync_RunsNoQueryUntilEnumeratedAndOnlyOneCommand()
        {
            CommandCounter counter = new();
            await using TestDbContext context = _database.CreateContext(counter);
            IReadRepository<Person> repository = new ReadRepository<Person>(context);

            IAsyncEnumerable<Person> stream = repository.StreamAsync(new Specification<Person> { AsNoTracking = true });
            int beforeEnumeration = counter.Asynchronous;

            int rows = 0;
            await foreach (Person _ in stream)
            {
                rows++;
            }

            Assert.Multiple(() =>
            {
                Assert.That(beforeEnumeration, Is.EqualTo(0));
                Assert.That(rows, Is.EqualTo(5));
                Assert.That(counter.Asynchronous, Is.EqualTo(1));
            });
        }

        [Test]
        public async Task StreamAsync_TokenCancelledDuringEnumeration_Throws()
        {
            await using TestDbContext context = _database.CreateContext();
            IReadRepository<Person> repository = new ReadRepository<Person>(context);
            using CancellationTokenSource cancellation = new();
            List<string> names = new();

            Assert.That(async () =>
            {
                await foreach (Person person in repository.StreamAsync(new Specification<Person> { AsNoTracking = true, OrderBy = q => q.OrderBy(p => p.Id) }, cancellation.Token))
                {
                    names.Add(person.Name);
                    cancellation.Cancel();
                }
            }, Throws.InstanceOf<OperationCanceledException>());

            Assert.That(names, Is.EqualTo(new[] { "Ada" }));
        }

        [Test]
        public async Task StreamAsync_WithCancellation_TokenCancelledDuringEnumeration_Throws()
        {
            await using TestDbContext context = _database.CreateContext();
            IReadRepository<Person> repository = new ReadRepository<Person>(context);
            using CancellationTokenSource cancellation = new();
            List<string> names = new();

            Assert.That(async () =>
            {
                await foreach (Person person in repository.StreamAsync(p => p.Age > 0, asNoTracking: true).WithCancellation(cancellation.Token))
                {
                    names.Add(person.Name);
                    cancellation.Cancel();
                }
            }, Throws.InstanceOf<OperationCanceledException>());

            Assert.That(names, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task StreamAsync_AlreadyCancelledToken_ThrowsBeforeAnyRow()
        {
            CommandCounter counter = new();
            await using TestDbContext context = _database.CreateContext(counter);
            IReadRepository<Person> repository = new ReadRepository<Person>(context);
            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();
            int rows = 0;

            Assert.That(async () =>
            {
                await foreach (Person _ in repository.StreamAsync(p => p.Age > 0, cancellationToken: cancellation.Token))
                {
                    rows++;
                }
            }, Throws.InstanceOf<OperationCanceledException>());

            Assert.Multiple(() =>
            {
                Assert.That(rows, Is.EqualTo(0));
                Assert.That(counter.Asynchronous, Is.EqualTo(0));
            });
        }

        [Test]
        public void StreamAsync_NullArguments_ThrowAtTheCall()
        {
            using TestDbContext context = _database.CreateContext();
            IReadRepository<Person> repository = new ReadRepository<Person>(context);

            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentNullException>(() => ((IReadRepository<Person>)null).StreamAsync(new Specification<Person>()));
                Assert.Throws<ArgumentNullException>(() => ((IReadRepository<Person>)null).StreamAsync(p => p.Age > 0));
                Assert.Throws<ArgumentNullException>(() => repository.StreamAsync((Specification<Person>)null));
                Assert.Throws<ArgumentNullException>(() => repository.StreamAsync((System.Linq.Expressions.Expression<Func<Person, bool>>)null));
            });
        }
    }
}
