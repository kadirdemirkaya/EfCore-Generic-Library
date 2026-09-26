using EfCore.Repository.Concretes;
using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class ReadRepositoryTests
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

        private static readonly Expression<Func<Person, PersonDto>> ToDto = p => new PersonDto
        {
            Id = p.Id,
            Name = p.Name,
            BasketCount = p.Baskets.Count
        };

        [Test]
        public async Task GetListAsync_ReturnsAllEntities()
        {
            List<Person> persons = await _repository.GetListAsync();

            Assert.That(persons, Has.Count.EqualTo(5));
        }

        [Test]
        public async Task GetListAsync_WithFilter_ReturnsMatchingEntities()
        {
            List<Person> persons = await _repository.GetListAsync(p => p.Age > 40);

            Assert.That(persons.Select(p => p.Name), Is.EquivalentTo(new[] { "Alan", "Grace", "Barbara" }));
        }

        [Test]
        public async Task GetListAsync_AsNoTracking_DoesNotTrackEntities()
        {
            await _repository.GetListAsync(true);

            Assert.That(_context.ChangeTracker.Entries<Person>(), Is.Empty);
        }

        [Test]
        public async Task GetListAsync_WithIncludes_LoadsNavigation()
        {
            List<Person> persons = await _repository.GetListAsync(p => p.Name == "Ada", q => q.Include(p => p.Baskets), true);

            Assert.That(persons.Single().Baskets, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GetListAsync_WithSpecification_AppliesConditionsOrderSkipAndTake()
        {
            Specification<Person> specification = new() { Skip = 1, Take = 2 };
            specification.Conditions.Add(p => p.Age > 30);
            specification.OrderBy = q => q.OrderBy(p => p.Age);

            List<Person> persons = await _repository.GetListAsync(specification);

            Assert.That(persons.Select(p => p.Name), Is.EqualTo(new[] { "Alan", "Barbara" }));
        }

        [Test]
        public async Task GetListAsync_WithProjection_MapsToDto()
        {
            List<PersonDto> persons = await _repository.GetListAsync(p => p.Name == "Ada", ToDto);

            Assert.That(persons.Single().BasketCount, Is.EqualTo(2));
        }

        [Test]
        public async Task GetListAsync_WithPaginationSpecification_ReturnsPage()
        {
            PaginationSpecification<Person> specification = new() { PageIndex = 2, PageSize = 2 };
            specification.OrderBy = q => q.OrderBy(p => p.Age);

            PaginatedList<Person> page = await _repository.GetListAsync(specification);

            Assert.Multiple(() =>
            {
                Assert.That(page.TotalItems, Is.EqualTo(5));
                Assert.That(page.TotalPages, Is.EqualTo(3));
                Assert.That(page.PageIndex, Is.EqualTo(2));
                Assert.That(page.Items.Select(p => p.Name), Is.EqualTo(new[] { "Alan", "Barbara" }));
            });
        }

        [Test]
        public async Task GetListAsync_WithPaginationAndProjection_ReturnsProjectedPage()
        {
            PaginationSpecification<Person> specification = new() { PageIndex = 1, PageSize = 3 };
            specification.Conditions.Add(p => p.Age < 50);
            specification.OrderBy = q => q.OrderBy(p => p.Name);

            PaginatedList<PersonDto> page = await _repository.GetListAsync(specification, ToDto);

            Assert.Multiple(() =>
            {
                Assert.That(page.TotalItems, Is.EqualTo(3));
                Assert.That(page.Items.Select(p => p.Name), Is.EqualTo(new[] { "Ada", "Alan", "Linus" }));
            });
        }

        [Test]
        public async Task GetByIdAsync_ReturnsEntity()
        {
            int id = _context.Persons.Single(p => p.Name == "Grace").Id;

            Person person = await _repository.GetByIdAsync(id);

            Assert.That(person.Name, Is.EqualTo("Grace"));
        }

        [Test]
        public async Task GetByIdAsync_ConvertsKeyType()
        {
            int id = _context.Persons.Single(p => p.Name == "Grace").Id;

            Person person = await _repository.GetByIdAsync(id.ToString(), true);

            Assert.That(person.Name, Is.EqualTo("Grace"));
        }

        [Test]
        public async Task GetByIdAsync_WithProjection_MapsToDto()
        {
            int id = _context.Persons.Single(p => p.Name == "Ada").Id;

            PersonDto person = await _repository.GetByIdAsync(id, ToDto, true);

            Assert.That(person.BasketCount, Is.EqualTo(2));
        }

        [Test]
        public void GetByIdAsync_NullId_Throws()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _repository.GetByIdAsync(null, false));
        }

        [Test]
        public async Task GetAsync_WithFilter_ReturnsFirstMatch()
        {
            Person person = await _repository.GetAsync(p => p.Age == 28);

            Assert.That(person.Name, Is.EqualTo("Linus"));
        }

        [Test]
        public async Task GetAsync_NoMatch_ReturnsNull()
        {
            Person person = await _repository.GetAsync(p => p.Age == 1000);

            Assert.That(person, Is.Null);
        }

        [Test]
        public async Task CountAsync_WithFilters_CombinesConditions()
        {
            int count = await _repository.CountAsync(new List<Expression<Func<Person, bool>>> { p => p.Age > 30, p => p.Age < 60 });

            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public async Task AnyAsync_ReflectsFilter()
        {
            Assert.Multiple(async () =>
            {
                Assert.That(await _repository.AnyAsync(p => p.Age > 80), Is.True);
                Assert.That(await _repository.AnyAsync(p => p.Age > 100), Is.False);
            });
        }

        [Test]
        public void Any_WithFilter_OutputsEntity()
        {
            bool exists = _repository.Any(out Person person, p => p.Name == "Alan");

            Assert.Multiple(() =>
            {
                Assert.That(exists, Is.True);
                Assert.That(person.Age, Is.EqualTo(41));
            });
        }

        [Test]
        public void Any_WithProjection_OutputsList()
        {
            bool exists = _repository.Any(out List<PersonDto> persons, p => p.Age > 40, ToDto);

            Assert.Multiple(() =>
            {
                Assert.That(exists, Is.True);
                Assert.That(persons, Has.Count.EqualTo(3));
            });
        }

        [Test]
        public async Task GetQueryable_IsComposable()
        {
            List<string> names = await _repository.GetQueryable().Where(p => p.Age < 30).Select(p => p.Name).ToListAsync();

            Assert.That(names, Is.EqualTo(new[] { "Linus" }));
        }

        [Test]
        public async Task ExecuteQueryAsync_WithParameters_ReturnsEntities()
        {
            List<Person> persons = await _repository.ExecuteQueryAsync("SELECT * FROM Persons WHERE Age > {0}", 50);

            Assert.That(persons, Has.Count.EqualTo(2));
        }
    }
}
