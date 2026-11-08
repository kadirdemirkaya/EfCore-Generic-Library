using EfCore.Repository.Abstractions;
using EfCore.Repository.Concretes;
using EfCore.Repository.Unit.Test.Data;
using EfCore.Repository.Unit.Test.Entities;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Repository.Unit.Test
{
    [TestFixture]
    public class MetadataKeyUpdateTests
    {
        private SqliteTestDatabase _database;

        [SetUp]
        public void SetUp() => _database = new SqliteTestDatabase();

        [TearDown]
        public void TearDown() => _database.Dispose();

        private int[] SeedShadowKeyItems(params string[] names)
        {
            using TestDbContext context = _database.CreateContext();
            ShadowKeyItem[] items = names.Select(n => new ShadowKeyItem { Name = n }).ToArray();
            context.ShadowKeyItems.AddRange(items);
            context.SaveChanges();
            return items.Select(i => (int)context.Entry(i).Property("Id").CurrentValue).ToArray();
        }

        private int[] SeedFieldKeyItems(params string[] names)
        {
            using TestDbContext context = _database.CreateContext();
            FieldKeyItem[] items = names.Select(n => new FieldKeyItem { Name = n }).ToArray();
            context.FieldKeyItems.AddRange(items);
            context.SaveChanges();
            return items.Select(i => i.KeyValue()).ToArray();
        }

        private string ShadowKeyItemName(int id)
        {
            using TestDbContext context = _database.CreateContext();
            return context.ShadowKeyItems.Where(i => EF.Property<int>(i, "Id") == id).Select(i => i.Name).Single();
        }

        private string FieldKeyItemName(int id)
        {
            using TestDbContext context = _database.CreateContext();
            return context.FieldKeyItems.Where(i => EF.Property<int>(i, "_id") == id).Select(i => i.Name).Single();
        }

        private static ShadowKeyItem DetachedShadowKeyItem(TestDbContext context, int id, string name)
        {
            ShadowKeyItem item = new() { Name = name };
            context.Entry(item).Property("Id").CurrentValue = id;
            return item;
        }

        [Test]
        public async Task UpdateAsync_ShadowKeySetOnEntry_Persists()
        {
            int[] ids = SeedShadowKeyItems("first");
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<ShadowKeyItem> repository = new(context);

            bool saved = await repository.UpdateAsync(DetachedShadowKeyItem(context, ids[0], "changed"));

            Assert.That(saved, Is.True);
            Assert.That(ShadowKeyItemName(ids[0]), Is.EqualTo("changed"));
        }

        [Test]
        public async Task Update_ShadowKeySetOnEntry_IsSavedBySaveChangesAsync()
        {
            int[] ids = SeedShadowKeyItems("first");
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<ShadowKeyItem> repository = new(context);

            repository.Update(DetachedShadowKeyItem(context, ids[0], "changed"));
            Assert.That(ShadowKeyItemName(ids[0]), Is.EqualTo("first"));

            await repository.SaveChangesAsync();

            Assert.That(ShadowKeyItemName(ids[0]), Is.EqualTo("changed"));
        }

        [Test]
        public async Task Update_ListOfShadowKeyEntities_IsSavedBySaveChangesAsync()
        {
            int[] ids = SeedShadowKeyItems("first", "second");
            await using TestDbContext context = _database.CreateContext();
            IWriteRepository<ShadowKeyItem> repository = new WriteRepository<ShadowKeyItem>(context);

            repository.Update(new[] { DetachedShadowKeyItem(context, ids[0], "one"), DetachedShadowKeyItem(context, ids[1], "two") });
            await repository.SaveChangesAsync();

            Assert.That(ShadowKeyItemName(ids[0]), Is.EqualTo("one"));
            Assert.That(ShadowKeyItemName(ids[1]), Is.EqualTo("two"));
        }

        [Test]
        public async Task UpdateAsync_ShadowKeyNeverSet_ThrowsAndTracksNothing()
        {
            SeedShadowKeyItems("first");
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<ShadowKeyItem> repository = new(context);
            ShadowKeyItem item = new() { Name = "changed" };

            InvalidOperationException exception = Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(item));

            Assert.That(exception.Message, Is.EqualTo("The primary key value of the entity to be updated is not valid."));
            Assert.That(context.ChangeTracker.Entries().Count(), Is.EqualTo(0));
            Assert.That(context.Entry(item).State, Is.EqualTo(EntityState.Detached));
        }

        [Test]
        public async Task UpdateAsync_FieldKey_Persists()
        {
            int[] ids = SeedFieldKeyItems("first");
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<FieldKeyItem> repository = new(context);

            bool saved = await repository.UpdateAsync(new FieldKeyItem(ids[0], "changed"));

            Assert.That(saved, Is.True);
            Assert.That(FieldKeyItemName(ids[0]), Is.EqualTo("changed"));
        }

        [Test]
        public async Task Update_FieldKeys_AreSavedBySaveChangesAsync()
        {
            int[] ids = SeedFieldKeyItems("first", "second", "third");
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<FieldKeyItem> repository = new(context);

            repository.Update(new FieldKeyItem(ids[0], "one"));
            repository.Update(new[] { new FieldKeyItem(ids[1], "two"), new FieldKeyItem(ids[2], "three") });
            await repository.SaveChangesAsync();

            Assert.That(FieldKeyItemName(ids[0]), Is.EqualTo("one"));
            Assert.That(FieldKeyItemName(ids[1]), Is.EqualTo("two"));
            Assert.That(FieldKeyItemName(ids[2]), Is.EqualTo("three"));
        }

        [Test]
        public async Task UpdateAsync_FieldKeyDefault_ThrowsAndTracksNothing()
        {
            int[] ids = SeedFieldKeyItems("first");
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<FieldKeyItem> repository = new(context);
            FieldKeyItem item = new(0, "changed");

            InvalidOperationException exception = Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(item));

            Assert.That(exception.Message, Is.EqualTo("The primary key value of the entity to be updated is not valid."));
            Assert.That(context.ChangeTracker.Entries().Count(), Is.EqualTo(0));
            Assert.That(FieldKeyItemName(ids[0]), Is.EqualTo("first"));
        }

        [Test]
        public async Task Update_ListWithOneDefaultFieldKey_AttachesNothing()
        {
            int[] ids = SeedFieldKeyItems("first");
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<FieldKeyItem> repository = new(context);

            Assert.Throws<InvalidOperationException>(() => repository.Update(new[] { new FieldKeyItem(ids[0], "one"), new FieldKeyItem(0, "two") }));

            Assert.That(context.ChangeTracker.Entries().Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task UpdateAsync_ClrKeyDefault_ThrowsAndLeavesEntityDetached()
        {
            await using TestDbContext context = _database.CreateContext();
            WriteRepository<Person> repository = new(context);
            Person person = new() { Name = "Ada", Age = 1 };

            Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(person));

            Assert.That(context.ChangeTracker.Entries().Count(), Is.EqualTo(0));
            Assert.That(context.Entry(person).State, Is.EqualTo(EntityState.Detached));
        }
    }
}
