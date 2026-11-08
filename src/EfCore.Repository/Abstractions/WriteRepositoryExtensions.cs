using EfCore.Repository.Concretes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace EfCore.Repository.Abstractions
{
    /// <summary>
    /// Write operations for any <see cref="IWriteRepository{TEntity}"/>, including the repositories returned by a unit
    /// of work: updates that mark entities as modified without saving, and set-based updates and deletes that run as one
    /// database command.
    /// </summary>
    public static class WriteRepositoryExtensions
    {
        /// <summary>
        /// Deletes every row of <typeparamref name="TEntity"/> that matches <paramref name="filter"/> with one database
        /// command, without loading the entities. The command runs immediately and bypasses the change tracker: no
        /// <c>SaveChangesAsync</c> is needed, pending tracked changes are neither saved nor discarded, and entities that
        /// are already tracked keep their state although their rows are gone. Global query filters of the entity type
        /// apply, so rows they hide are not deleted. When the context has an active transaction (for example from
        /// <c>BeginTransactionAsync</c> or <see cref="IUnitOfWork.ExecuteInTransactionAsync"/>), the command joins it and
        /// a rollback undoes it.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="repository">The repository whose context runs the command.</param>
        /// <param name="filter">The condition the rows to delete must match.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The number of deleted rows.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="filter"/> is <see langword="null"/>.</exception>
        public static Task<int> DeleteWhereAsync<TEntity>(
            this IWriteRepository<TEntity> repository,
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default)
            where TEntity : class
            => FilteredSet(repository, filter).ExecuteDeleteAsync(cancellationToken);

        /// <summary>
        /// Deletes every row of <typeparamref name="TEntity"/> that matches all <see cref="SpecificationBase{TEntity}.Conditions"/>
        /// of <paramref name="specification"/>, with the same behaviour as
        /// <see cref="DeleteWhereAsync{TEntity}(IWriteRepository{TEntity}, Expression{Func{TEntity, bool}}, CancellationToken)"/>.
        /// Includes, ordering and the no-tracking flag of the specification do not affect which rows are deleted.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="repository">The repository whose context runs the command.</param>
        /// <param name="specification">The specification whose conditions the rows to delete must match.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The number of deleted rows.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="repository"/> or <paramref name="specification"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The specification has no conditions or a <see langword="null"/> condition, is a <see cref="PaginationSpecification{TEntity}"/>, or is a <see cref="Specification{TEntity}"/> with <c>Skip</c> or <c>Take</c> set.</exception>
        public static Task<int> DeleteWhereAsync<TEntity>(
            this IWriteRepository<TEntity> repository,
            SpecificationBase<TEntity> specification,
            CancellationToken cancellationToken = default)
            where TEntity : class
            => SpecifiedSet(repository, specification).ExecuteDeleteAsync(cancellationToken);

        /// <summary>
        /// Updates every row of <typeparamref name="TEntity"/> that matches <paramref name="filter"/> with one database
        /// command, without loading the entities; <paramref name="setters"/> is passed unchanged to the provider's
        /// set-based update, for example <c>s =&gt; s.SetProperty(p =&gt; p.Age, p =&gt; p.Age + 1)</c>. The command runs
        /// immediately and bypasses the change tracker: no <c>SaveChangesAsync</c> is needed, pending tracked changes are
        /// neither saved nor discarded, and entities that are already tracked keep their old values. Global query filters
        /// of the entity type apply, so rows they hide are not updated. When the context has an active transaction (for
        /// example from <c>BeginTransactionAsync</c> or <see cref="IUnitOfWork.ExecuteInTransactionAsync"/>), the command
        /// joins it and a rollback undoes it.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="repository">The repository whose context runs the command.</param>
        /// <param name="filter">The condition the rows to update must match.</param>
        /// <param name="setters">The properties to set and their new values.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The number of updated rows.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="repository"/>, <paramref name="filter"/> or <paramref name="setters"/> is <see langword="null"/>.</exception>
        public static Task<int> UpdateWhereAsync<TEntity>(
            this IWriteRepository<TEntity> repository,
            Expression<Func<TEntity, bool>> filter,
#if NET10_0_OR_GREATER
            Action<UpdateSettersBuilder<TEntity>> setters,
#else
            Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setters,
#endif
            CancellationToken cancellationToken = default)
            where TEntity : class
        {
            IQueryable<TEntity> query = FilteredSet(repository, filter);

            if (setters == null)
            {
                throw new ArgumentNullException(nameof(setters));
            }

            return query.ExecuteUpdateAsync(setters, cancellationToken);
        }

        /// <summary>
        /// Updates every row of <typeparamref name="TEntity"/> that matches all <see cref="SpecificationBase{TEntity}.Conditions"/>
        /// of <paramref name="specification"/>, with the same behaviour as the filter overload of <c>UpdateWhereAsync</c>.
        /// Includes, ordering and the no-tracking flag of the specification do not affect which rows are updated.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="repository">The repository whose context runs the command.</param>
        /// <param name="specification">The specification whose conditions the rows to update must match.</param>
        /// <param name="setters">The properties to set and their new values.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The number of updated rows.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="repository"/>, <paramref name="specification"/> or <paramref name="setters"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The specification has no conditions or a <see langword="null"/> condition, is a <see cref="PaginationSpecification{TEntity}"/>, or is a <see cref="Specification{TEntity}"/> with <c>Skip</c> or <c>Take</c> set.</exception>
        public static Task<int> UpdateWhereAsync<TEntity>(
            this IWriteRepository<TEntity> repository,
            SpecificationBase<TEntity> specification,
#if NET10_0_OR_GREATER
            Action<UpdateSettersBuilder<TEntity>> setters,
#else
            Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setters,
#endif
            CancellationToken cancellationToken = default)
            where TEntity : class
        {
            IQueryable<TEntity> query = SpecifiedSet(repository, specification);

            if (setters == null)
            {
                throw new ArgumentNullException(nameof(setters));
            }

            return query.ExecuteUpdateAsync(setters, cancellationToken);
        }

        private static IQueryable<TEntity> FilteredSet<TEntity>(IWriteRepository<TEntity> repository, Expression<Func<TEntity, bool>> filter)
            where TEntity : class
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return repository.Table.Set<TEntity>().Where(filter);
        }

        private static IQueryable<TEntity> SpecifiedSet<TEntity>(IWriteRepository<TEntity> repository, SpecificationBase<TEntity> specification)
            where TEntity : class
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            if (specification == null)
            {
                throw new ArgumentNullException(nameof(specification));
            }

            if (specification is PaginationSpecification<TEntity>)
            {
                throw new ArgumentException("A pagination specification cannot select the rows of a set-based update or delete.", nameof(specification));
            }

            if (specification is Specification<TEntity> limited && (limited.Skip != null || limited.Take != null))
            {
                throw new ArgumentException("Skip and Take cannot limit the rows of a set-based update or delete.", nameof(specification));
            }

            if (specification.Conditions == null || specification.Conditions.Count == 0)
            {
                throw new ArgumentException("The specification needs at least one condition for a set-based update or delete.", nameof(specification));
            }

            if (specification.Conditions.Any(condition => condition == null))
            {
                throw new ArgumentException("The specification cannot contain null conditions.", nameof(specification));
            }

            IQueryable<TEntity> query = repository.Table.Set<TEntity>();

            foreach (Expression<Func<TEntity, bool>> condition in specification.Conditions)
            {
                query = query.Where(condition);
            }

            return query;
        }

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
