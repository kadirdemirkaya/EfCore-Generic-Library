using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EfCore.Repository.Concretes
{
    internal static class EntityUpdate
    {
        internal static void EnsureValidPrimaryKey<TEntity>(DbContext dbContext, TEntity entity)
            where TEntity : class
        {
            IEntityType? entityType = dbContext.Model.FindEntityType(typeof(TEntity));

            if (entityType == null)
            {
                throw new InvalidOperationException($"{typeof(TEntity).Name} is not part of EF Core DbContext model");
            }

            IReadOnlyList<IProperty>? keyProperties = entityType.FindPrimaryKey()?.Properties;

            if (keyProperties == null || keyProperties.Count == 0)
            {
                return;
            }

            EntityEntry<TEntity> entry = dbContext.Entry(entity);
            bool anyKeyPartNull = false;
            bool allKeyPartsDefault = true;

            foreach (IProperty keyProperty in keyProperties)
            {
                object? keyDefaultValue = keyProperty.ClrType.IsValueType ? Activator.CreateInstance(keyProperty.ClrType) : null;

                object? keyValue = entry.Property(keyProperty.Name).CurrentValue;

                anyKeyPartNull |= keyValue == null;
                allKeyPartsDefault &= Equals(keyDefaultValue, keyValue);
            }

            if (anyKeyPartNull || allKeyPartsDefault)
            {
                throw new InvalidOperationException("The primary key value of the entity to be updated is not valid.");
            }
        }

        internal static void MarkModified<TEntity>(DbContext dbContext, TEntity entity)
            where TEntity : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (dbContext.ChangeTracker.Entries<TEntity>().Any(x => x.Entity == entity))
            {
                return;
            }

            EnsureValidPrimaryKey(dbContext, entity);
            dbContext.Set<TEntity>().Update(entity);
        }

        internal static void MarkModified<TEntity>(DbContext dbContext, IEnumerable<TEntity> entities)
            where TEntity : class
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            List<TEntity> list = entities.ToList();

            if (list.Any(entity => entity == null))
            {
                throw new ArgumentNullException(nameof(entities), "The list cannot contain null entities.");
            }

            HashSet<TEntity> tracked = new(dbContext.ChangeTracker.Entries<TEntity>().Select(x => x.Entity), ReferenceEqualityComparer.Instance);
            List<TEntity> untracked = list.Where(entity => !tracked.Contains(entity)).ToList();

            foreach (TEntity entity in untracked)
            {
                EnsureValidPrimaryKey(dbContext, entity);
            }

            dbContext.Set<TEntity>().UpdateRange(untracked);
        }
    }
}
