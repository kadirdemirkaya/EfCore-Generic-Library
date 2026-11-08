using EfCore.Repository.Abstractions;
using EfCore.Repository.Concretes;
using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class DeferredUpdateTests
    {
        private SqliteTestDatabase _database;

        [SetUp]
        public void SetUp() => _database = new SqliteTestDatabase();

        [TearDown]
        public void TearDown() => _database.Dispose();

        private TestDbContext CreateContext(SaveChangesCounter counter)
            => new(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_database.ConnectionString).AddInterceptors(counter).Options);

        private int[] SeedPersons(params string[] names)
        {
            Person[] persons = names.Select(n => new Person { Name = n, Age = 1 }).ToArray();
            _database.Seed(persons);
            return persons.Select(p => p.Id).ToArray();
        }

        private string NameOf(int id)
        {
            using TestDbContext context = _database.CreateContext();
            return context.Persons.Single(p => p.Id == id).Name;
        }

        private int SeedNote()
        {
            using TestDbContext context = _database.CreateContext();
            VersionedNote note = new() { Text = "draft", Version = 1 };
            context.VersionedNotes.Add(note);
            context.SaveChanges();
            return note.Id;
        }

        private void ChangeNoteElsewhere(int id)
        {
            using TestDbContext context = _database.CreateContext();
            VersionedNote note = context.VersionedNotes.Single(n => n.Id == id);
            note.Text = "other writer";
            note.Version = 2;
            context.SaveChanges();
        }

        [Test]
        public async Task Update_UntrackedEntity_DoesNotSaveUntilSaveChangesAsync()
        {
            int[] ids = SeedPersons("Ada");
            SaveChangesCounter counter = new();
            await using TestDbContext context = CreateContext(counter);
            WriteRepository<Person> repository = new(context);

            repository.Update(new Person { Id = ids[0], Name = "Ada Lovelace", Age = 37 });

            Assert.Multiple(() =>
            {
                Assert.That(counter.Calls, Is.EqualTo(0));
                Assert.That(NameOf(ids[0]), Is.EqualTo("Ada"));
                Assert.That(context.ChangeTracker.Entries<Person>().Single().State, Is.EqualTo(EntityState.Modified));
            });

            bool saved = await repository.SaveChangesAsync();

            Assert.Multiple(() =>
            {
                Assert.That(saved, Is.True);
                Assert.That(counter.Calls, Is.EqualTo(1));
                Assert.That(NameOf(ids[0]), Is.EqualTo("Ada Lovelace"));
            });
        }

        [Test]
        public async Task Update_Range_WritesEveryEntityInOneSaveChanges()
        {
            int[] ids = SeedPersons("Ada", "Alan", "Grace");
            SaveChangesCounter counter = new();
            await using TestDbContext context = CreateContext(counter);
            WriteRepository<Person> repository = new(context);

            repository.Update(ids.Select(id => new Person { Id = id, Name = $"updated-{id}", Age = 2 }).ToList());

            Assert.That(counter.Calls, Is.EqualTo(0));
            Assert.That(await repository.SaveChangesCountAsync(), Is.EqualTo(3));

            Assert.Multiple(() =>
            {
                Assert.That(counter.Calls, Is.EqualTo(1));
                foreach (int id in ids)
                {
                    Assert.That(NameOf(id), Is.EqualTo($"updated-{id}"));
                }
            });
        }

        [Test]
        public async Task Update_TrackedEntity_IsSavedWithItsChanges()
        {
            int[] ids = SeedPersons("Ada");
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<Person> repository = new(context);
            Person person = context.Persons.Single(p => p.Id == ids[0]);
            person.Name = "Countess";

            repository.Update(person);

            Assert.That(await repository.SaveChangesAsync(), Is.True);
            Assert.That(NameOf(ids[0]), Is.EqualTo("Countess"));
        }

        [Test]
        public async Task Update_DefaultKey_ThrowsTheSameErrorAsUpdateAsync()
        {
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<Person> repository = new(context);

            InvalidOperationException fromUpdateAsync = Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(new Person { Name = "Nobody" }));
            InvalidOperationException fromUpdate = Assert.Throws<InvalidOperationException>(() => repository.Update(new Person { Name = "Nobody" }));

            Assert.Multiple(() =>
            {
                Assert.That(fromUpdate.Message, Is.EqualTo(fromUpdateAsync.Message));
                Assert.That(context.ChangeTracker.Entries().Any(), Is.False);
            });
        }

        [Test]
        public async Task Update_NullStringKey_Throws()
        {
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<Tag> repository = new(context);

            Assert.Throws<InvalidOperationException>(() => repository.Update(new Tag { Label = "orphan" }));
        }

        [Test]
        public async Task Update_CompositeKey_FirstPartDefault_IsSaved()
        {
            await using (TestDbContext seed = _database.CreateContext())
            {
                seed.OrderLines.Add(new OrderLine { OrderId = 0, LineNumber = 1, Product = "mouse" });
                await seed.SaveChangesAsync();
            }

            await using TestDbContext context = _database.CreateContext();
            WriteRepository<OrderLine> repository = new(context);

            repository.Update(new OrderLine { OrderId = 0, LineNumber = 1, Product = "trackball" });
            Assert.That(await repository.SaveChangesAsync(), Is.True);

            await using TestDbContext verify = _database.CreateContext();
            Assert.That(verify.OrderLines.Single().Product, Is.EqualTo("trackball"));
        }

        [Test]
        public async Task Update_CompositeKey_AllPartsDefault_Throws()
        {
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<OrderLine> repository = new(context);

            Assert.Throws<InvalidOperationException>(() => repository.Update(new OrderLine { Product = "orphan" }));
        }

        [Test]
        public async Task Update_Range_InvalidKey_ThrowsAndAttachesNothing()
        {
            int[] ids = SeedPersons("Ada");
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<Person> repository = new(context);

            List<Person> entities = new()
            {
                new Person { Id = ids[0], Name = "changed", Age = 2 },
                new Person { Name = "no key" }
            };

            Assert.Throws<InvalidOperationException>(() => repository.Update(entities));
            Assert.That(context.ChangeTracker.Entries().Any(), Is.False);
            Assert.That(await repository.SaveChangesAsync(), Is.False);
            Assert.That(NameOf(ids[0]), Is.EqualTo("Ada"));
        }

        [Test]
        public async Task Update_NullArguments_Throw()
        {
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<Person> repository = new(context);

            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentNullException>(() => repository.Update((Person)null));
                Assert.Throws<ArgumentNullException>(() => repository.Update((IEnumerable<Person>)null));
                Assert.Throws<ArgumentNullException>(() => repository.Update(new List<Person> { null }));
            });
            Assert.That(context.ChangeTracker.Entries().Any(), Is.False);
        }

        [Test]
        public async Task Update_ThroughUnitOfWork_WritesWithOtherEntitiesInOneSave()
        {
            int[] ids = SeedPersons("Ada");
            SaveChangesCounter counter = new();
            await using TestDbContext context = CreateContext(counter);
            IUnitOfWork unitOfWork = new UnitOfWork(context);

            unitOfWork.GetWriteRepository<Person>().Update(new Person { Id = ids[0], Name = "Ada Lovelace", Age = 37 });
            unitOfWork.GetWriteRepository<Tag>().Add(new Tag { Code = "t1", Label = "one" });

            Assert.That(counter.Calls, Is.EqualTo(0));
            Assert.That(await unitOfWork.SaveChangesAsync(), Is.True);

            await using TestDbContext verify = _database.CreateContext();
            Assert.Multiple(() =>
            {
                Assert.That(counter.Calls, Is.EqualTo(1));
                Assert.That(NameOf(ids[0]), Is.EqualTo("Ada Lovelace"));
                Assert.That(verify.Tags.Count(), Is.EqualTo(1));
            });
        }

        [Test]
        public async Task Update_ThroughWriteRepositoryInterface_RangeAndInvalidKey()
        {
            int[] ids = SeedPersons("Ada", "Alan");
            await using TestDbContext context = _database.CreateContext();
            IWriteRepository<Person> repository = new UnitOfWork(context).GetWriteRepository<Person>();

            Assert.Throws<InvalidOperationException>(() => repository.Update(new Person { Name = "no key" }));
            Assert.Throws<ArgumentNullException>(() => ((IWriteRepository<Person>)null).Update(new Person { Id = 1 }));

            repository.Update(ids.Select(id => new Person { Id = id, Name = "same", Age = 3 }));

            Assert.That(await repository.SaveChangesCountAsync(), Is.EqualTo(2));
            Assert.That(ids.Select(NameOf), Is.All.EqualTo("same"));
        }

        [Test]
        public async Task Update_ThroughUnitOfWorkTransaction_IsCommitted()
        {
            int[] ids = SeedPersons("Ada");
            await using TestDbContext context = _database.CreateContext();
            IUnitOfWork unitOfWork = new UnitOfWork(context);

            await unitOfWork.ExecuteInTransactionAsync(_ =>
            {
                unitOfWork.GetWriteRepository<Person>().Update(new Person { Id = ids[0], Name = "in transaction", Age = 4 });
                return Task.CompletedTask;
            });

            Assert.That(NameOf(ids[0]), Is.EqualTo("in transaction"));
        }

        [Test]
        public async Task Update_StaleConcurrencyToken_SaveChangesThrowsConcurrencyException()
        {
            int id = SeedNote();
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<VersionedNote> repository = new(context);

            ChangeNoteElsewhere(id);
            repository.Update(new VersionedNote { Id = id, Text = "stale writer", Version = 1 });

            Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repository.SaveChangesAsync());

            await using TestDbContext verify = _database.CreateContext();
            Assert.That(verify.VersionedNotes.Single().Text, Is.EqualTo("other writer"));
        }

        [Test]
        public async Task UpdateAsync_StaleConcurrencyToken_ThrowsConcurrencyException()
        {
            int id = SeedNote();
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<VersionedNote> repository = new(context);
            VersionedNote note = context.VersionedNotes.Single(n => n.Id == id);

            ChangeNoteElsewhere(id);
            note.Text = "stale writer";

            Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => repository.UpdateAsync(note));

            await using TestDbContext verify = _database.CreateContext();
            Assert.That(verify.VersionedNotes.Single().Text, Is.EqualTo("other writer"));
        }

        [Test]
        public async Task UnitOfWorkSave_StaleConcurrencyToken_ThrowsConcurrencyException()
        {
            int id = SeedNote();
            await using TestDbContext context = _database.CreateContext();
            IUnitOfWork unitOfWork = new UnitOfWork(context);

            ChangeNoteElsewhere(id);
            unitOfWork.GetWriteRepository<VersionedNote>().Update(new VersionedNote { Id = id, Text = "stale writer", Version = 1 });

            Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => unitOfWork.SaveChangesAsync());
        }

        [Test]
        public async Task Update_CurrentConcurrencyToken_IsSaved()
        {
            int id = SeedNote();
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<VersionedNote> repository = new(context);

            repository.Update(new VersionedNote { Id = id, Text = "current writer", Version = 1 });

            Assert.That(await repository.SaveChangesAsync(), Is.True);
        }
    }
}
