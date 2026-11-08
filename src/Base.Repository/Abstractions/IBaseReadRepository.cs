using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Base.Repository.Abstractions
{
    public interface IBaseReadRepository<TEntity> : ITable
        where TEntity : class
    {
        /// <summary>
        /// Runs a raw SQL query and maps the rows to <typeparamref name="TEntity"/>.
        /// Every value is passed as a database parameter through a <c>{0}</c>, <c>{1}</c>, ... placeholder;
        /// never build <paramref name="query"/> by concatenating or interpolating user input.
        /// </summary>
        /// <param name="query">The SQL text with <c>{0}</c>-style placeholders.</param>
        /// <param name="parameters">The values bound to the placeholders as database parameters.</param>
        /// <returns>The materialized entities.</returns>
        Task<List<TEntity>> ExecuteQueryAsync(string query, params object[] parameters);

        /// <summary>
        /// Runs an interpolated SQL query and maps the rows to <typeparamref name="TEntity"/>.
        /// Every interpolated value is sent as a database parameter, never inlined into the SQL text.
        /// </summary>
        /// <param name="query">The SQL as an interpolated string, for example <c>$"SELECT * FROM Persons WHERE Name = {name}"</c>.</param>
        /// <param name="cancellationToken">A token to cancel the query.</param>
        /// <returns>The materialized entities.</returns>
        Task<List<TEntity>> ExecuteQueryInterpolatedAsync(FormattableString query, CancellationToken cancellationToken = default);
        IQueryable<TEntity> GetQueryable();
        Task<List<TEntity>> GetListAsync(CancellationToken cancellationToken = default);
        Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(CancellationToken cancellationToken = default);
        Task<int> CountAsync(CancellationToken cancellationToken = default);

    }
}
