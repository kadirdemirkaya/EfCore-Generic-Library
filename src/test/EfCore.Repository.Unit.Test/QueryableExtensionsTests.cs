using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class QueryableExtensionsTests
    {
        private SqliteTestDatabase _database;
        private TestDbContext _context;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();
            _database.Seed(SqliteTestDatabase.SamplePersons());
            _context = _database.CreateContext();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
            _database.Dispose();
        }

        [Test]
        public async Task ToPaginatedListAsync_ReturnsRequestedPage()
        {
            PaginatedList<Person> page = await _context.Persons.OrderBy(p => p.Age).ToPaginatedListAsync(3, 2);

            Assert.Multiple(() =>
            {
                Assert.That(page.TotalItems, Is.EqualTo(5));
                Assert.That(page.TotalPages, Is.EqualTo(3));
                Assert.That(page.Items.Single().Name, Is.EqualTo("Grace"));
            });
        }

        [Test]
        public async Task ToPaginatedListAsync_LargePageSize_ReturnsAllItems()
        {
            PaginatedList<Person> page = await _context.Persons.ToPaginatedListAsync(1, int.MaxValue);

            Assert.Multiple(() =>
            {
                Assert.That(page.Items, Has.Count.EqualTo(5));
                Assert.That(page.TotalPages, Is.EqualTo(1));
            });
        }

        [TestCase(0, 10)]
        [TestCase(1, 0)]
        public void ToPaginatedListAsync_InvalidArguments_Throw(int pageIndex, int pageSize)
        {
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _context.Persons.ToPaginatedListAsync(pageIndex, pageSize));
        }

        [Test]
        public void GetSpecifiedQuery_NegativeSkip_Throws()
        {
            Specification<Person> specification = new() { Skip = -1 };

            Assert.Throws<ArgumentOutOfRangeException>(() => _context.Persons.GetSpecifiedQuery(specification));
        }

        [Test]
        public void GetSpecifiedQuery_NullSpecification_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _context.Persons.GetSpecifiedQuery((SpecificationBase<Person>)null));
        }
    }
}
