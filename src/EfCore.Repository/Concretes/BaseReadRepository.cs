using Base.Repository.Abstractions;
using Base.Repository.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EfCore.Repository.Concretes
{
    public class BaseReadRepository<TEntity> : IBaseReadRepository<TEntity>
        where TEntity : class, new()
    {
        public DbContext _dbContext { get; set; }
        public IServiceProvider _serviceProvider { get; set; }
        public DatabaseOptions _databaseOptions { get; set; }

        public BaseReadRepository(DbContext dbContext)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(_dbContext), "DatabaseOptions cannot be null");
            }
            _dbContext = dbContext;
        }

        public BaseReadRepository(DbContext dbContext, IServiceProvider serviceProvider)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(_dbContext), "DatabaseOptions cannot be null");
            }
            _dbContext = dbContext;
            _serviceProvider = serviceProvider;
        }

        public BaseReadRepository(DatabaseOptions databaseOptions, IServiceProvider serviceProvider)
        {
            if (databaseOptions == null)
            {
                throw new ArgumentNullException(nameof(databaseOptions), "DatabaseOptions cannot be null");
            }
            _databaseOptions = databaseOptions;
            if (databaseOptions.Connection == null)
            {
                throw new ArgumentNullException(nameof(databaseOptions.Connection), "DatabaseOptions.Connection cannot be null");
            }
            _serviceProvider = serviceProvider;
            _dbContext = (DbContext)databaseOptions.Connection ?? throw new InvalidOperationException("DbContext is not initialized properly.");
        }

        public DbContext Table
        {
            get
            {
                return _dbContext;
            }
        }

        public async Task<List<TEntity>> ExecuteQueryAsync(string query)
            => await _dbContext.Set<TEntity>().FromSqlRaw(query).ToListAsync();

        public async Task<List<TEntity>> ExecuteQueryAsync(string query, params object[] parameters)
            => await _dbContext.Set<TEntity>().FromSqlRaw(query, parameters).ToListAsync();

        public IQueryable<TEntity> GetQueryable()
          => _dbContext.Set<TEntity>();

        public async Task<List<TEntity>> GetListAsync(CancellationToken cancellationToken = default)
          => await _dbContext.Set<TEntity>().ToListAsync();

        public async Task<TEntity> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            return await GetByIdAsync(id, false, cancellationToken);
        }
        private async Task<TEntity> GetByIdAsync(object id, bool asNoTracking, CancellationToken cancellationToken = default)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            IEntityType entityType = _dbContext.Model.FindEntityType(typeof(TEntity));

            string primaryKeyName = entityType.FindPrimaryKey().Properties.Select(p => p.Name).FirstOrDefault();
            Type primaryKeyType = entityType.FindPrimaryKey().Properties.Select(p => p.ClrType).FirstOrDefault();

            if (primaryKeyName == null || primaryKeyType == null)
                throw new ArgumentException("Entity does not have any primary key defined", nameof(id));

            object primaryKeyValue = null;

            try
            {
                primaryKeyValue = Convert.ChangeType(id, primaryKeyType, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                throw new ArgumentException($"You can not assign a value of type {id.GetType()} to a property of type {primaryKeyType}");
            }

            ParameterExpression pe = Expression.Parameter(typeof(TEntity), "entity");
            MemberExpression me = Expression.Property(pe, primaryKeyName);
            ConstantExpression constant = Expression.Constant(primaryKeyValue, primaryKeyType);
            BinaryExpression body = Expression.Equal(me, constant);
            Expression<Func<TEntity, bool>> expressionTree = Expression.Lambda<Func<TEntity, bool>>(body, new[] { pe });

            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(expressionTree, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
            => await _dbContext.Set<TEntity>().AnyAsync(cancellationToken).ConfigureAwait(false);

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
            => await _dbContext.Set<TEntity>().CountAsync(cancellationToken).ConfigureAwait(false);
    }
}
