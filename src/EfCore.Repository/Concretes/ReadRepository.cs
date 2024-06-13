using Base.Repository.Options;
using EfCore.Repository.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace EfCore.Repository.Concretes
{
    public class ReadRepository<TEntity> : BaseReadRepository<TEntity>, IReadRepository<TEntity>
          where TEntity : class, new()
    {
        public DbContext _dbContext { get; set; }
        public IServiceProvider? _serviceProvider { get; set; }
        public DatabaseOptions? _databaseOptions { get; set; }

        public ReadRepository(DbContext dbContext)
            : base(dbContext)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(_dbContext), "DatabaseOptions cannot be null");
            }
            _dbContext = dbContext;
        }

        public ReadRepository(DatabaseOptions databaseOptions, IServiceProvider serviceProvider)
            : base(databaseOptions, serviceProvider)
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

        public ReadRepository(DbContext dbContext, IServiceProvider serviceProvider)
            : base(dbContext, serviceProvider)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(_dbContext), "DatabaseOptions cannot be null");
            }
            _dbContext = dbContext;
            _serviceProvider = serviceProvider;
        }

        public DbContext Table
        {
            get
            {
                return _dbContext;
            }
        }

        #region Any

        /// <summary>
        /// then will update  !!!
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool Any(out TEntity entity)
        {
            entity = null;

            using (var context = _dbContext)
            {
                var dbContext = context.Set<TEntity>();
                var idProperty = typeof(TEntity).GetProperty("Id");

                if (idProperty != null)
                {
                    var entityModel = new TEntity();
                    idProperty.SetValue(entityModel, 1);

                    bool exists = dbContext.Any(e => EF.Property<int>(e, "Id") == EF.Property<int>(entityModel, "Id"));

                    if (exists)
                    {
                        entity = dbContext.FirstOrDefault(e => EF.Property<int>(e, "Id") == EF.Property<int>(entityModel, "Id"));
                    }

                    return exists;
                }
                return false;
            }
        }

        public bool Any(out TEntity entity, Expression<Func<TEntity, bool>> filter, bool asNoTracking = false)
        {
            entity = null;

            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            bool exists = query.Any();

            if (exists)
            {
                entity = query.FirstOrDefault();
            }

            return exists;
        }

        public bool Any(out List<TEntity> entity, Expression<Func<TEntity, bool>> filter, bool asNoTracking = false)
        {
            entity = null;

            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            bool exists = query.Any();

            if (exists)
            {
                entity = query.ToList();
            }

            return exists;
        }

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> filter, bool asNoTracking = false, CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }
            return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> AnyAsync(IEnumerable<Expression<Func<TEntity, bool>>> filters, bool asNoTracking = false, CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            if (filters != null)
            {
                foreach (Expression<Func<TEntity, bool>> expression in filters)
                {
                    query = query.Where(expression);
                }
            }

            return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
        }

        public bool Any(out List<TEntity> entities, Expression<Func<TEntity, bool>> filter, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includes, bool asNoTracking = false)
        {
            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (includes != null)
            {
                query = includes(query);
            }

            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            entities = query.ToList();

            return entities is not null && entities.Count() > 0 ? true : false;
        }

        public bool Any<MapType>(out List<MapType> mapTypes, Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, MapType>> mapFilter)
        {
            if (mapFilter == null)
            {
                throw new ArgumentNullException(nameof(mapFilter));
            }

            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            mapTypes = query.Select(mapFilter).ToList();

            return mapTypes is not null && mapTypes.Count() > 0 ? true : false;
        }

        public bool Any<MapType>(out PaginatedList<MapType> paginatedList, PaginationSpecification<TEntity> specification, Expression<Func<TEntity, MapType>> mapFilter, CancellationToken cancellationToken = default) where MapType : class
        {
            if (specification == null)
            {
                throw new ArgumentNullException(nameof(specification));
            }

            if (mapFilter == null)
            {
                throw new ArgumentNullException(nameof(mapFilter));
            }

            IQueryable<TEntity> query = _dbContext.Set<TEntity>().GetSpecifiedQuery((SpecificationBase<TEntity>)specification);

            paginatedList = query.Select(mapFilter)
                .ToPaginatedListAsync(specification.PageIndex, specification.PageSize, cancellationToken).GetAwaiter().GetResult();

            return paginatedList is not null ? true : false;
        }

        public bool Any<MapType>(out List<MapType> mapTypes, Specification<TEntity> specification, Expression<Func<TEntity, MapType>> mapFilter)
        {
            if (mapFilter == null)
            {
                throw new ArgumentNullException(nameof(mapFilter));
            }

            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (specification != null)
            {
                query = query.GetSpecifiedQuery(specification);
            }

            mapTypes = query.Select(mapFilter).ToList();

            return mapTypes is not null && mapTypes.Count() > 0 ? true : false;
        }

        #endregion 


        #region Get
        public async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
            => await GetAsync(filter, null, false, cancellationToken);

        public async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> filter, bool asNoTracking, CancellationToken cancellationToken = default)
            => await GetAsync(filter, null, asNoTracking, cancellationToken);

        public async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> filter, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includes, CancellationToken cancellationToken = default)
            => await GetAsync(filter, includes, false, cancellationToken);

        public async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> filter, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includes, bool asNoTracking, CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (filter != null)
                query = query.Where(filter);

            if (includes != null)
                query = includes(query);

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<TEntity> GetAsync(Specification<TEntity> specification, CancellationToken cancellationToken = default)
            => await GetAsync(specification, false, cancellationToken);

        public async Task<TEntity> GetAsync(Specification<TEntity> specification, bool asNoTracking, CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (specification != null)
                query = query.GetSpecifiedQuery(specification);

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<MapType> GetAsync<MapType>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, MapType>> mapFilter, CancellationToken cancellationToken = default)
        {
            if (mapFilter == null)
                throw new ArgumentNullException(nameof(mapFilter));

            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (filter != null)
                query = query.Where(filter);

            return await query.Select(mapFilter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<MapType> GetAsync<MapType>(Specification<TEntity> specification, Expression<Func<TEntity, MapType>> mapFilter, CancellationToken cancellationToken = default)
        {
            if (mapFilter == null)
                throw new ArgumentNullException(nameof(mapFilter));

            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (specification != null)
                query = query.GetSpecifiedQuery(specification);

            return await query.Select(mapFilter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }
        #endregion

        #region GetById
        public async Task<TEntity> GetByIdAsync(object id, bool asNoTracking, CancellationToken cancellationToken = default)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            return await GetByIdAsync(id, null, asNoTracking, cancellationToken);
        }

        public async Task<TEntity> GetByIdAsync(object id, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includes, CancellationToken cancellationToken = default)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            return await GetByIdAsync(id, includes, false, cancellationToken);
        }

        public async Task<TEntity> GetByIdAsync(object id, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includes, bool asNoTracking, CancellationToken cancellationToken = default)
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

            if (includes != null)
                query = includes(query);

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(expressionTree, cancellationToken).ConfigureAwait(false);
        }

        public async Task<MapType> GetByIdAsync<MapType>(object id, Expression<Func<TEntity, MapType>> mapFilter, bool asNoTracking, CancellationToken cancellationToken = default)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (mapFilter == null)
                throw new ArgumentNullException(nameof(mapFilter));

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

            return await query.Where(expressionTree)
                              .Select(mapFilter)
                              .FirstOrDefaultAsync(cancellationToken)
                              .ConfigureAwait(false);
        }
        #endregion

        #region Count
        public async Task<int> CountAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
            => await _dbContext.Set<TEntity>().CountAsync(cancellationToken).ConfigureAwait(false);


        public async Task<int> CountAsync(IEnumerable<Expression<Func<TEntity, bool>>> filters, CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (filters != null)
                foreach (Expression<Func<TEntity, bool>> expression in filters)
                    query = query.Where(expression);

            return await query.CountAsync(cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region GetList
        public async Task<List<TEntity>> GetListAsync(bool asNoTracking, CancellationToken cancellationToken = default)
        {
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> nullValue = null;
            return await GetListAsync(nullValue, asNoTracking, cancellationToken);
        }

        public async Task<List<TEntity>> GetListAsync(Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includes, CancellationToken cancellationToken = default)
            => await GetListAsync(includes, false, cancellationToken);

        public async Task<List<TEntity>> GetListAsync(Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includes, bool asNoTracking, CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (includes != null)
                query = includes(query);

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (filter != null)
                query = query.Where(filter);

            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> filter, bool asNoTracking, CancellationToken cancellationToken = default)
            => await GetListAsync(filter, false, cancellationToken);

        public async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> filter, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> includes, bool asNoTracking = false, CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (filter != null)
                query = query.Where(filter);

            if (includes != null)
                query = includes(query);

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<TEntity>> GetListAsync(Specification<TEntity> specification, CancellationToken cancellationToken = default)
            => await GetListAsync(specification, false, cancellationToken);

        public async Task<List<TEntity>> GetListAsync(Specification<TEntity> specification, bool asNoTracking, CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (specification != null)
                query = query.GetSpecifiedQuery(specification);

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<MapType>> GetListAsync<MapType>(Expression<Func<TEntity, MapType>> mapFilter, CancellationToken cancellationToken = default)
        {
            if (mapFilter == null)
                throw new ArgumentNullException(nameof(mapFilter));

            return await _dbContext.Set<TEntity>()
                .Select(mapFilter).ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<MapType>> GetListAsync<MapType>(Expression<Func<TEntity, bool>> filter, Expression<Func<TEntity, MapType>> mapFilter, CancellationToken cancellationToken = default)
        {
            if (mapFilter == null)
                throw new ArgumentNullException(nameof(mapFilter));

            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (filter != null)
                query = query.Where(filter);

            return await query.Select(mapFilter)
               .ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<MapType>> GetListAsync<MapType>(Specification<TEntity> specification, Expression<Func<TEntity, MapType>> mapFilter, CancellationToken cancellationToken = default)
        {
            if (mapFilter == null)
                throw new ArgumentNullException(nameof(mapFilter));

            IQueryable<TEntity> query = _dbContext.Set<TEntity>();

            if (specification != null)
                query = query.GetSpecifiedQuery(specification);

            return await query.Select(mapFilter)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<PaginatedList<TEntity>> GetListAsync(PaginationSpecification<TEntity> specification, CancellationToken cancellationToken = default)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            return await _dbContext.Set<TEntity>().ToPaginatedListAsync(specification, cancellationToken);
        }

        public async Task<PaginatedList<MapType>> GetListAsync<MapType>(PaginationSpecification<TEntity> specification, Expression<Func<TEntity, MapType>> mapFilter, CancellationToken cancellationToken = default) where MapType : class
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            if (mapFilter == null)
                throw new ArgumentNullException(nameof(mapFilter));

            IQueryable<TEntity> query = _dbContext.Set<TEntity>().GetSpecifiedQuery((SpecificationBase<TEntity>)specification);

            return await query.Select(mapFilter)
                .ToPaginatedListAsync(specification.PageIndex, specification.PageSize, cancellationToken);
        }
        #endregion
    }
}
