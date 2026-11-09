using Base.Repository.Abstractions;
using EfCore.Repository.Abstractions;
using EfCore.Repository.Concretes;
using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class RawSqlCommandTests
    {
        private SqliteTestDatabase _database;
        private NonQueryRecorder _recorder;
        private TestDbContext _context;
        private WriteRepository<Person> _repository;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();
            _database.Seed(SqliteTestDatabase.SamplePersons());
            _recorder = new NonQueryRecorder();
            _context = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_database.ConnectionString)
                .AddInterceptors(_recorder)
                .Options);
            _repository = new WriteRepository<Person>(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
            _database.Dispose();
        }

        private Dictionary<string, int> AgesByName()
        {
            using TestDbContext context = _database.CreateContext();
            return context.Persons.AsNoTracking().ToDictionary(p => p.Name ?? "<null>", p => p.Age);
        }

        private List<string> Names()
        {
            using TestDbContext context = _database.CreateContext();
            return context.Persons.AsNoTracking().OrderBy(p => p.Id).Select(p => p.Name).ToList();
        }

        [Test]
        public async Task ExecuteCommandInterpolatedAsync_SendsValuesAsParameters()
        {
            int age = 99;
            string name = "Ada";

            bool executed = await _repository.ExecuteCommandInterpolatedAsync($"UPDATE Persons SET Age = {age} WHERE Name = {name}");

            Assert.That(executed, Is.True);
            Assert.That(AgesByName()["Ada"], Is.EqualTo(99));
            Assert.That(_recorder.CommandTexts, Has.Count.EqualTo(1));
            Assert.That(_recorder.CommandTexts[0], Does.Not.Contain("99").And.Not.Contain("Ada"));
            Assert.That(_recorder.ParameterValues[0], Is.EqualTo(new object[] { 99, "Ada" }));
        }

        [Test]
        public async Task ExecuteCommandInterpolatedAsync_InjectionPayloadInFilter_IsTreatedAsValue()
        {
            Dictionary<string, int> before = AgesByName();
            string name = "x' OR '1'='1";

            bool executed = await _repository.ExecuteCommandInterpolatedAsync($"UPDATE Persons SET Age = 0 WHERE Name = {name}");

            Assert.That(executed, Is.False);
            Assert.That(AgesByName(), Is.EqualTo(before));
            Assert.That(_recorder.CommandTexts[0], Does.Not.Contain("OR"));
        }

        [Test]
        public async Task ExecuteCommandInterpolatedAsync_StatementInjectionPayload_IsStoredVerbatim()
        {
            string payload = "Ada'; DELETE FROM Persons; --";

            bool executed = await _repository.ExecuteCommandInterpolatedAsync($"UPDATE Persons SET Name = {payload} WHERE Name = {"Ada"}");

            Assert.That(executed, Is.True);
            Assert.That(Names(), Has.Count.EqualTo(5));
            Assert.That(Names(), Does.Contain(payload).And.Not.Contain("Ada"));
        }

        [Test]
        public async Task ExecuteCommandInterpolatedAsync_ValueWithQuote_MatchesStoredValue()
        {
            _database.Seed(new Person { Name = "O'Neil", Age = 60 });
            string name = "O'Neil";

            bool executed = await _repository.ExecuteCommandInterpolatedAsync($"UPDATE Persons SET Age = {61} WHERE Name = {name}");

            Assert.That(executed, Is.True);
            Assert.That(AgesByName()["O'Neil"], Is.EqualTo(61));
        }

        [Test]
        public async Task ExecuteCommandInterpolatedAsync_NullValue_IsWrittenAsDatabaseNull()
        {
            string name = null;

            bool executed = await _repository.ExecuteCommandInterpolatedAsync($"UPDATE Persons SET Name = {name} WHERE Age = {85}");

            Assert.That(executed, Is.True);
            Assert.That(Names(), Is.EqualTo(new[] { "Ada", "Alan", null, "Linus", "Barbara" }));
        }

        [Test]
        public async Task ExecuteCommandInterpolatedAsync_NoRowAffected_ReturnsFalse()
        {
            bool executed = await _repository.ExecuteCommandInterpolatedAsync($"DELETE FROM Persons WHERE Age > {200}");

            Assert.That(executed, Is.False);
            Assert.That(Names(), Has.Count.EqualTo(5));
        }

        [Test]
        public void ExecuteCommandInterpolatedAsync_CanceledToken_Throws()
        {
            int age = 1;

            Assert.That(
                async () => await _repository.ExecuteCommandInterpolatedAsync($"UPDATE Persons SET Age = {age}", new CancellationToken(true)),
                Throws.InstanceOf<OperationCanceledException>());
            Assert.That(AgesByName().Values, Has.None.EqualTo(1));
        }

        [Test]
        public void ExecuteCommandInterpolatedAsync_NullCommand_Throws()
        {
            Assert.That(
                async () => await _repository.ExecuteCommandInterpolatedAsync(null),
                Throws.InstanceOf<ArgumentNullException>().With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("command"));
        }

        [Test]
        public async Task ExecuteCommandInterpolatedAsync_RolledBackTransaction_DiscardsTheCommand()
        {
            string name = "Ada";

            await using (IDbContextTransaction transaction = await _repository.BeginTransactionAsync())
            {
                bool executed = await _repository.ExecuteCommandInterpolatedAsync($"DELETE FROM Persons WHERE Name = {name}");
                Assert.That(executed, Is.True);
                await transaction.RollbackAsync();
            }

            Assert.That(Names(), Does.Contain("Ada"));
        }

        [Test]
        public async Task ExecuteCommandInterpolatedAsync_CommittedTransaction_KeepsTheCommand()
        {
            string name = "Ada";

            await using (IDbContextTransaction transaction = await _repository.BeginTransactionAsync())
            {
                await _repository.ExecuteCommandInterpolatedAsync($"DELETE FROM Persons WHERE Name = {name}");
                await transaction.CommitAsync();
            }

            Assert.That(Names(), Does.Not.Contain("Ada"));
        }

        [Test]
        public async Task Extension_ThroughUnitOfWorkRepository_SendsValuesAsParameters()
        {
            IUnitOfWork unitOfWork = new UnitOfWork(_context);
            IWriteRepository<Person> repository = unitOfWork.GetWriteRepository<Person>();
            string name = "x' OR '1'='1";

            bool executed = await repository.ExecuteCommandInterpolatedAsync($"DELETE FROM Persons WHERE Name = {name}");

            Assert.That(executed, Is.False);
            Assert.That(Names(), Has.Count.EqualTo(5));
            Assert.That(_recorder.ParameterValues[0], Is.EqualTo(new object[] { name }));
        }

        [Test]
        public void Extension_FailingUnitOfWorkTransaction_RollsTheCommandBack()
        {
            IUnitOfWork unitOfWork = new UnitOfWork(_context);
            IWriteRepository<Person> repository = unitOfWork.GetWriteRepository<Person>();
            string name = "Ada";

            Assert.That(
                async () => await unitOfWork.ExecuteInTransactionAsync(async cancellationToken =>
                {
                    await repository.ExecuteCommandInterpolatedAsync($"DELETE FROM Persons WHERE Name = {name}", cancellationToken);
                    throw new InvalidOperationException("fail after the command");
                }),
                Throws.InvalidOperationException);
            Assert.That(Names(), Does.Contain("Ada"));
        }

        [Test]
        public async Task Extension_ThroughBaseInterface_WorksOnBaseWriteRepository()
        {
            IBaseWriteRepository<Person> repository = new BaseWriteRepository<Person>(_context);
            int age = 30;

            bool executed = await repository.ExecuteCommandInterpolatedAsync($"UPDATE Persons SET Age = {age} WHERE Age < {age}");

            Assert.That(executed, Is.True);
            Assert.That(AgesByName()["Linus"], Is.EqualTo(30));
            Assert.That(_recorder.ParameterValues[0], Is.EqualTo(new object[] { 30, 30 }));
        }

        [Test]
        public void Extension_InvalidArguments_Throw()
        {
            IWriteRepository<Person> repository = _repository;

            Assert.That(
                () => WriteRepositoryExtensions.ExecuteCommandInterpolatedAsync<Person>(null, $"DELETE FROM Persons"),
                Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("repository"));
            Assert.That(
                () => repository.ExecuteCommandInterpolatedAsync(null),
                Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("command"));
        }

        [Test]
        public void Extension_CanceledToken_Throws()
        {
            IWriteRepository<Person> repository = _repository;
            int age = 1;

            Assert.That(
                async () => await repository.ExecuteCommandInterpolatedAsync($"UPDATE Persons SET Age = {age}", new CancellationToken(true)),
                Throws.InstanceOf<OperationCanceledException>());
            Assert.That(AgesByName().Values, Has.None.EqualTo(1));
        }

        [Test]
        public async Task ExecuteCommandAsync_WithParameterArray_StillBindsParameters()
        {
            string name = "x' OR '1'='1";

            bool executed = await _repository.ExecuteCommandAsync("DELETE FROM Persons WHERE Name = {0}", name);

            Assert.That(executed, Is.False);
            Assert.That(Names(), Has.Count.EqualTo(5));
        }
    }
}
