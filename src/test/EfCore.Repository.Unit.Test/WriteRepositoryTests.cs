using EfCore.Repository.Concretes;
using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class WriteRepositoryTests
    {
        private SqliteTestDatabase _database;
        private TestDbContext _context;
        private WriteRepository<Person> _repository;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();
            _context = _database.CreateContext();
            _repository = new WriteRepository<Person>(_context);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
            _database.Dispose();
        }

        private int CountPersons()
        {
            using TestDbContext context = _database.CreateContext();
            return context.Persons.Count();
        }

        [Test]
        public async Task AddAsync_ThenSaveChangesAsync_Persists()
        {
            await _repository.AddAsync(new Person { Name = "Ada", Age = 36 });

            bool saved = await _repository.SaveChangesAsync();

            Assert.Multiple(() =>
            {
                Assert.That(saved, Is.True);
                Assert.That(CountPersons(), Is.EqualTo(1));
            });
        }

        [Test]
        public async Task Add_Range_ThenSaveChangesCountAsync_ReturnsAffectedRows()
        {
            _repository.Add(new[] { new Person { Name = "Ada" }, new Person { Name = "Alan" } });

            int affected = await _repository.SaveChangesCountAsync();

            Assert.That(affected, Is.EqualTo(2));
        }

        [Test]
        public async Task InsertAsync_ReturnsGeneratedKey()
        {
            object[] keys = await _repository.InsertAsync(new Person { Name = "Ada", Age = 36 });

            Assert.That(keys, Has.Length.EqualTo(1));
            Assert.That(keys[0], Is.GreaterThan(0));
        }

        [Test]
        public async Task InsertAsync_List_ReturnsKeyPerEntity()
        {
            object[] keys = await _repository.InsertAsync(new List<Person> { new() { Name = "Ada" }, new() { Name = "Alan" } });

            Assert.Multiple(() =>
            {
                Assert.That(keys, Has.Length.EqualTo(2));
                Assert.That(CountPersons(), Is.EqualTo(2));
            });
        }

        [Test]
        public async Task UpdateAsync_UntrackedEntity_Persists()
        {
            object[] keys = await _repository.InsertAsync(new Person { Name = "Ada", Age = 36 });
            _repository.ClearChangeTracker();

            bool updated = await _repository.UpdateAsync(new Person { Id = (int)keys[0], Name = "Ada Lovelace", Age = 37 });

            using TestDbContext context = _database.CreateContext();
            Assert.Multiple(() =>
            {
                Assert.That(updated, Is.True);
                Assert.That(context.Persons.Single().Name, Is.EqualTo("Ada Lovelace"));
            });
        }

        [Test]
        public void UpdateAsync_DefaultKey_Throws()
        {
            Assert.ThrowsAsync<InvalidOperationException>(() => _repository.UpdateAsync(new Person { Name = "Nobody" }));
        }

        [Test]
        public async Task UpdateAsync_UntrackedEntityWithStringKey_Persists()
        {
            WriteRepository<Tag> repository = new(_context);
            await repository.InsertAsync(new Tag { Code = "net", Label = "dotnet" });
            repository.ClearChangeTracker();

            bool updated = await repository.UpdateAsync(new Tag { Code = "net", Label = ".NET" });

            using TestDbContext context = _database.CreateContext();
            Assert.Multiple(() =>
            {
                Assert.That(updated, Is.True);
                Assert.That(context.Tags.Single().Label, Is.EqualTo(".NET"));
            });
        }

        [Test]
        public void UpdateAsync_NullStringKey_Throws()
        {
            WriteRepository<Tag> repository = new(_context);

            Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(new Tag { Label = "orphan" }));
        }

        [Test]
        public async Task DeleteAsync_RemovesEntity()
        {
            Person person = new() { Name = "Ada" };
            await _repository.InsertAsync(person);

            bool deleted = await _repository.DeleteAsync(person);

            Assert.Multiple(() =>
            {
                Assert.That(deleted, Is.True);
                Assert.That(CountPersons(), Is.EqualTo(0));
            });
        }

        [Test]
        public async Task ExecuteCommandAsync_WithParameters_AffectsRows()
        {
            await _repository.InsertAsync(new Person { Name = "Ada", Age = 36 });

            bool executed = await _repository.ExecuteCommandAsync("UPDATE Persons SET Age = {0}", 40);

            Assert.That(executed, Is.True);
        }

        [Test]
        public async Task BeginTransactionAsync_Rollback_DiscardsChanges()
        {
            await using (IDbContextTransaction transaction = await _repository.BeginTransactionAsync())
            {
                await _repository.InsertAsync(new Person { Name = "Ada" });
                await transaction.RollbackAsync();
            }

            Assert.That(CountPersons(), Is.EqualTo(0));
        }

        [Test]
        public async Task BeginTransactionAsync_WithIsolationLevel_UsesRequestedLevel()
        {
            await using IDbContextTransaction transaction = await _repository.BeginTransactionAsync(IsolationLevel.ReadUncommitted);

            Assert.That(transaction.GetDbTransaction().IsolationLevel, Is.EqualTo(IsolationLevel.ReadUncommitted));
        }

        [Test]
        public async Task BeginTransactionAsync_Unspecified_UsesProviderDefault()
        {
            await using IDbContextTransaction transaction = await _repository.BeginTransactionAsync();

            Assert.That(transaction.GetDbTransaction().IsolationLevel, Is.EqualTo(IsolationLevel.Serializable));
        }

        [Test]
        public void BeginTransactionAsync_CanceledToken_Throws()
        {
            Assert.CatchAsync<OperationCanceledException>(() => _repository.BeginTransactionAsync(IsolationLevel.ReadUncommitted, new CancellationToken(true)));
        }

        [Test]
        public void AddAsync_Null_Throws()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddAsync((Person)null));
        }
    }
}
