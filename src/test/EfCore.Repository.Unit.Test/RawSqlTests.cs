using Base.Repository.Abstractions;
using EfCore.Repository.Concretes;
using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class RawSqlTests
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

        [Test]
        public async Task ExecuteQueryInterpolatedAsync_SendsValuesAsParameters()
        {
            int minimumAge = 50;

            List<Person> persons = await _repository.ExecuteQueryInterpolatedAsync($"SELECT * FROM Persons WHERE Age > {minimumAge}");

            Assert.That(persons.Select(p => p.Name), Is.EquivalentTo(new[] { "Grace", "Barbara" }));
        }

        [Test]
        public async Task ExecuteQueryInterpolatedAsync_InjectionPayload_IsTreatedAsValue()
        {
            string name = "'' OR 1=1";

            List<Person> persons = await _repository.ExecuteQueryInterpolatedAsync($"SELECT * FROM Persons WHERE Name = {name}");

            Assert.That(persons, Is.Empty);
        }

        [Test]
        public async Task ExecuteQueryInterpolatedAsync_ValueWithQuote_MatchesStoredValue()
        {
            _database.Seed(new Person { Name = "O'Neil", Age = 60 });
            string name = "O'Neil";

            List<Person> persons = await _repository.ExecuteQueryInterpolatedAsync($"SELECT * FROM Persons WHERE Name = {name}");

            Assert.That(persons.Select(p => p.Name), Is.EqualTo(new[] { "O'Neil" }));
        }

        [Test]
        public async Task ExecuteQueryInterpolatedAsync_ThroughBaseInterface_IsComposableWithEveryRepository()
        {
            IBaseReadRepository<Person> repository = new BaseReadRepository<Person>(_context);
            int maximumAge = 30;

            List<Person> persons = await repository.ExecuteQueryInterpolatedAsync($"SELECT * FROM Persons WHERE Age < {maximumAge}");

            Assert.That(persons.Select(p => p.Name), Is.EqualTo(new[] { "Linus" }));
        }

        [Test]
        public void ExecuteQueryInterpolatedAsync_CanceledToken_Throws()
        {
            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();
            int minimumAge = 0;

            Assert.That(
                async () => await _repository.ExecuteQueryInterpolatedAsync($"SELECT * FROM Persons WHERE Age > {minimumAge}", cancellation.Token),
                Throws.InstanceOf<OperationCanceledException>());
        }

        [Test]
        public void ExecuteQueryInterpolatedAsync_NullQuery_Throws()
        {
            Assert.That(
                async () => await _repository.ExecuteQueryInterpolatedAsync(null),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public async Task ExecuteQueryAsync_WithParameterArray_StillBindsParameters()
        {
            string name = "'' OR 1=1";

            List<Person> persons = await _repository.ExecuteQueryAsync("SELECT * FROM Persons WHERE Name = {0}", name);

            Assert.That(persons, Is.Empty);
        }
    }
}
