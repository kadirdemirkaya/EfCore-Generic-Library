using EfCore.Repository.Concretes;

namespace EfCore.Repository.Abstractions
{
    /// <summary>
    /// Update operations for any <see cref="IWriteRepository{TEntity}"/>, including the repositories returned by a unit
    /// of work, that mark entities as modified without saving.
    /// </summary>
    public static class WriteRepositoryExtensions
    {
        /// <summary>
        /// Marks <paramref name="entity"/> as modified on the repository's context without saving; the change is written
        /// by the next <c>SaveChangesAsync</c> of the repository or of the unit of work sharing the context. A tracked
        /// entity is left as it is; an untracked entity is attached after the same primary key check as <c>UpdateAsync</c>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="repository">The repository whose context tracks the change.</param>
        /// <param name="entity">The entity to update.</param>
        /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="entity"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The entity is untracked and a primary key part is <see langword="null"/> or every part holds its default value.</exception>
        public static void Update<TEntity>(this IWriteRepository<TEntity> repository, TEntity entity)
            where TEntity : class
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            EntityUpdate.MarkModified(repository.Table, entity);
        }

        /// <summary>
        /// Marks every entity of <paramref name="entities"/> as modified on the repository's context without saving, with
        /// the same rules as <see cref="Update{TEntity}(IWriteRepository{TEntity}, TEntity)"/>. Every untracked entity is
        /// checked before any of them is attached, so an invalid key leaves the context unchanged.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="repository">The repository whose context tracks the changes.</param>
        /// <param name="entities">The entities to update.</param>
        /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="entities"/> is <see langword="null"/>, or <paramref name="entities"/> contains <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">An untracked entity has a primary key part that is <see langword="null"/> or only default key values.</exception>
        public static void Update<TEntity>(this IWriteRepository<TEntity> repository, IEnumerable<TEntity> entities)
            where TEntity : class
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            EntityUpdate.MarkModified(repository.Table, entities);
        }
    }
}
