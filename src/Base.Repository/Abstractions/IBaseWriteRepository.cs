using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Base.Repository.Abstractions
{
    public interface IBaseWriteRepository<TEntity> : ITable
        where TEntity : class
    {
        /// <summary>
        /// Runs <paramref name="command"/> as raw SQL without any database parameter: the text is sent exactly as given.
        /// Never build it by concatenating or interpolating user input — an interpolated string passed here is formatted
        /// into plain text, so its values become part of the SQL. For commands with values, use
        /// <c>ExecuteCommandInterpolatedAsync</c> (<c>EfCore.Repository.Abstractions.WriteRepositoryExtensions</c>, also an
        /// instance method of the repository classes), which sends every interpolated value as a database parameter.
        /// </summary>
        /// <param name="command">The complete SQL text.</param>
        /// <param name="cancellationToken">A token to cancel the command.</param>
        /// <returns><see langword="true"/> when at least one row was affected; otherwise <see langword="false"/>.</returns>
        Task<bool> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs a raw SQL command whose values are passed as database parameters through <c>{0}</c>, <c>{1}</c>, ...
        /// placeholders. Never build <paramref name="command"/> by concatenating or interpolating user input: an
        /// interpolated string passed here is formatted into plain text, so its values are inlined into the SQL. To write
        /// the values directly in an interpolated string, use <c>ExecuteCommandInterpolatedAsync</c>
        /// (<c>EfCore.Repository.Abstractions.WriteRepositoryExtensions</c>), which sends them as database parameters.
        /// </summary>
        /// <param name="command">The SQL text with <c>{0}</c>-style placeholders.</param>
        /// <param name="parameters">The values bound to the placeholders as database parameters.</param>
        /// <returns><see langword="true"/> when at least one row was affected; otherwise <see langword="false"/>.</returns>
        Task<bool> ExecuteCommandAsync(string command, params object[] parameters);

        /// <summary>
        /// Runs a raw SQL command whose values are passed as database parameters through <c>{0}</c>, <c>{1}</c>, ...
        /// placeholders. Never build <paramref name="command"/> by concatenating or interpolating user input: an
        /// interpolated string passed here is formatted into plain text, so its values are inlined into the SQL. To write
        /// the values directly in an interpolated string, use <c>ExecuteCommandInterpolatedAsync</c>
        /// (<c>EfCore.Repository.Abstractions.WriteRepositoryExtensions</c>), which sends them as database parameters.
        /// </summary>
        /// <param name="command">The SQL text with <c>{0}</c>-style placeholders.</param>
        /// <param name="parameters">The values bound to the placeholders as database parameters.</param>
        /// <param name="cancellationToken">A token to cancel the command.</param>
        /// <returns><see langword="true"/> when at least one row was affected; otherwise <see langword="false"/>.</returns>
        Task<bool> ExecuteCommandAsync(string command, IEnumerable<object> parameters, CancellationToken cancellationToken = default);


        Task<IDbContextTransaction> BeginTransactionAsync(
             IsolationLevel isolationLevel = IsolationLevel.Unspecified,
             CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

        void Remove(TEntity entity);

        void ClearChangeTracker();

        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        Task<bool> SaveChangesAsync();
    }
}
