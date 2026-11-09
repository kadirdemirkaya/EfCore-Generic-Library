# Changelog

The repository ships two packages: `EfCore.RepositoryPack` (`src/EfCore.Repository`) and `EfCore.BaseRepositoryPack`
(`src/Base.Repository`). Up to 1.3.0 they were versioned independently; from 2.0.0 on both packages always share one
version number. The release number and the NuGet publish are chosen by the repository owner.

## Unreleased

### Added

- `IUnitOfWork` (no type argument) and its implementation `UnitOfWork`: `GetReadRepository<TEntity>()` and `GetWriteRepository<TEntity>()` for any entity on one shared context, `SaveChangesAsync(CancellationToken)` that writes the changes of every entity in one call, and `ExecuteInTransactionAsync(Func<CancellationToken, Task>, IsolationLevel, CancellationToken)`, which runs the delegate in a transaction through the provider's execution strategy, saves and commits, and on failure rolls back and clears the change tracker. Inside an open transaction the delegate joins it.
- `AddEfCoreRepository<TEntity, TDbContext>` also registers `IUnitOfWork` on the container-resolved context (first call wins; existing registrations are unchanged).
- `Update(TEntity)` and `Update(IEnumerable<TEntity>)`, which mark entities as modified without saving so the next `SaveChangesAsync` writes them together with other changes: instance methods on `WriteRepository<TEntity>` and extension methods on `IWriteRepository<TEntity>` (`WriteRepositoryExtensions`, namespace `EfCore.Repository.Abstractions`), so they also work on repositories returned by a unit of work. Untracked entities get the same primary key check as `UpdateAsync`; a list is checked before anything is attached. `IWriteRepository<TEntity>` itself is unchanged.
- `DeleteWhereAsync` and `UpdateWhereAsync` extension methods on `IWriteRepository<TEntity>` (`WriteRepositoryExtensions`): delete or update every row matching a filter expression or the conditions of a `SpecificationBase<TEntity>` with one SQL command and return the affected row count. They bypass the change tracker, respect global query filters and join the context's current transaction. The setters parameter is passed to EF Core's set-based update unchanged, so its type follows the target framework (`Action<UpdateSettersBuilder<TEntity>>` on `net10.0`, `Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>>` on `net8.0`/`net9.0`). Specifications without conditions, with a `null` condition, with `Skip`/`Take`, or of type `PaginationSpecification<TEntity>` are rejected with `ArgumentException`. `IWriteRepository<TEntity>` itself is unchanged.
- Query options on `SpecificationBase<TEntity>` (so on `Specification<TEntity>` and `PaginationSpecification<TEntity>` too): `AsSplitQuery`, `IgnoreQueryFilters` and `AsNoTrackingWithIdentityResolution`, all `false` by default (unchanged queries). They are applied by `SpecificationEvaluator.GetSpecifiedQuery` and therefore by every repository method that takes a specification; `IgnoreQueryFilters` also applies to the total count of paginated reads, including `QueryableExtensions.ToPaginatedListAsync(PaginationSpecification)`. `AsNoTrackingWithIdentityResolution` takes precedence over `AsNoTracking`. The set-based `DeleteWhereAsync` / `UpdateWhereAsync` still use only the specification's conditions, and global query filters always apply to them.
- `StreamAsync` extension methods on `IReadRepository<TEntity>` (`ReadRepositoryExtensions`, namespace `EfCore.Repository.Abstractions`): `StreamAsync(Specification<TEntity>, CancellationToken)` and `StreamAsync(Expression<Func<TEntity, bool>>, bool asNoTracking, CancellationToken)` return an `IAsyncEnumerable<TEntity>` that reads rows one at a time; the query runs when enumeration starts and the token (or `WithCancellation`) cancels it. `IReadRepository<TEntity>` itself is unchanged.
- `PaginatedList<TEntity>.HasPreviousPage` (`PageIndex > 1`) and `HasNextPage` (`PageIndex < TotalPages`), computed from the existing properties.
- Keyset (cursor) pagination: `GetPageAfterAsync` extension methods on `IReadRepository<TEntity>` (`ReadRepositoryExtensions`) order the rows by one key selected with `Expression<Func<TEntity, TKey>>` (`TKey` implementing `IComparable<TKey>`, value or reference type) and return the rows after a cursor (`null` for the first page), ascending or descending, filtered by an optional filter expression (with `asNoTracking`) or by a `SpecificationBase<TEntity>` (conditions, includes and query options; a specification with `OrderBy`, `Skip`/`Take` or a `PaginationSpecification<TEntity>` is rejected with `ArgumentException`). The result type `KeysetPage<TEntity, TKey>` carries `Items`, `PageSize`, `HasNextPage` and `NextCursor`. One query per page, no total count; composite keys are not supported. `IReadRepository<TEntity>` itself is unchanged.
- README: documents that `DbUpdateConcurrencyException` for entities with a concurrency token is passed through unchanged by the save and update methods (no behaviour change).
- `SaveChangesAsync(CancellationToken)` overloads on `UnitOfWork<TEntity>` and `BaseWriteRepository<TEntity>`. The interfaces `IUnitOfWork<TEntity>` and `IBaseWriteRepository<TEntity>` are unchanged, so existing implementations of them keep compiling.

### Changed

- The assembly-scanning `EfCoreRepositoryServiceRegistration<TBaseType, TDbContext>(lifetime, assemblies)` builds the constructor call of every registered repository, unit of work and context-aware entity once at registration time instead of locating it through reflection on every resolve. Resolved types, lifetimes, the context and service provider passed to them, and the exception thrown by a failing constructor (`TargetInvocationException` wrapping the original) are unchanged; resolving is roughly three times faster.

### Fixed

- The primary key check of `UpdateAsync(TEntity)` and `Update(TEntity)` / `Update(IEnumerable<TEntity>)` reads the key values through the EF Core model instead of looking up a public property of the same name. Keys mapped to a field or to a shadow property (value set with `context.Entry(entity).Property(name).CurrentValue`) no longer fail with "The primary key value of the entity to be updated is not valid."; the check still rejects `null` or all-default key values and leaves a rejected entity untracked. Keys on public properties behave as before.

## 2.0.0 - 2026-11-08 (both packages)

Everything since `EfCore.RepositoryPack` 1.3.0 / `EfCore.BaseRepositoryPack` 1.2.0, released as one major version.

### Breaking changes

- **Target frameworks:** both packages target `net8.0`, `net9.0` and `net10.0`; `net6.0` and `net7.0` are dropped. Migration: move the application to .NET 8 or later, or stay on 1.x.
- **Dependencies:** each target framework references the current patched EF Core and Microsoft.Extensions versions (`8.0.33`, `9.0.22`, `10.0.14`; Microsoft.Extensions on `net8.0`: Configuration.Abstractions `8.0.0`, Configuration.Binder `8.0.2`, DependencyInjection `8.0.1`, DependencyInjection.Abstractions `8.0.2`, Options `8.0.2`). Applications referencing lower EF Core patch versions are lifted to these.
- **Raw SQL:** `IBaseReadRepository<TEntity>.ExecuteQueryAsync(string query)` and its implementation on `BaseReadRepository<TEntity>` (and so on `ReadRepository` / `DbReadRepository`) are removed: the unparameterized string overload invited SQL built from user input. Migration: `ExecuteQueryInterpolatedAsync($"... {value}")` (new, values are sent as database parameters) or `ExecuteQueryAsync(sql, parameters)` with `{0}`-style placeholders. `ExecuteQueryAsync(string, params object[])` is unchanged, so an existing `ExecuteQueryAsync(sql)` call still compiles against it after recompiling: review every such call and never interpolate values into its `sql` string. Custom implementations of `IBaseReadRepository<TEntity>` must implement `ExecuteQueryInterpolatedAsync`.
- **Context members:** the public `_dbContext` property of `BaseReadRepository<TEntity>`, `BaseWriteRepository<TEntity>`, `ReadRepository<TEntity>` (`new`), `WriteRepository<TEntity>` (`new`), `Table` and `UnitOfWork<TEntity>` is removed (now a private field). Migration: the repositories' public `Table` property (`ITable.Table`) returns the same context; for a unit of work use `unitOfWork.GetTable().Table`; for `Table` use `((ITable)table).Table`.
- **`DatabaseOptions`:** `RetryCount` and `IsRetry` are removed; no code ever read them. Migration: delete the assignments (configure retries on the provider, for example in `AddDbContext`).
- **`ParameterValidationAttribute`** (`EfCore.Repository.Attributes`) is removed; nothing in the packages used it. Migration: delete the usages.
- **`ConfigurationHelpers.GetOptions<TModel>(IServiceCollection, string)`** is removed: it built an intermediate service provider from the collection during registration. Migration: `configuration.GetOptions<TModel>(section)` on the `IConfiguration` (for example `builder.Configuration`), which is unchanged.
- **Nullable annotations:** `GetByIdAsync` and `GetAsync` return `Task<TEntity?>` / `Task<MapType?>`; `Any(out TEntity?)` and `Any(out List<TEntity>?, ...)` carry `[NotNullWhen(true)]`; `SpecificationBase.Includes` / `OrderBy` and the repositories' and unit of work's `_serviceProvider` / `_databaseOptions` are nullable. No runtime change; projects with nullable reference types enabled may see new warnings where these results are used without a null check.

### Added

- `ExecuteQueryInterpolatedAsync(FormattableString query, CancellationToken cancellationToken = default)` on `IBaseReadRepository<TEntity>` and every read repository: runs an interpolated SQL query and sends each interpolated value as a database parameter.
- `LICENSE.txt` and this changelog.

### Fixed

- Entities registered one by one with `EfCoreRepositoryServiceRegistration<TEntity, TDbContext>(lifetime)` now share one context per `TDbContext`, so a unit of work saves changes made through another entity's repositories.
- The assembly-scan registration no longer registers the marker type as a service, so resolving it no longer throws and an existing registration of the marker type is kept.
- `InsertAsync(List<TEntity>)` adds the whole list and saves it with a single `SaveChanges`, so the insert is all-or-nothing instead of leaving earlier entities saved when a later one fails; a list containing `null` throws `ArgumentNullException` before anything is saved. The returned keys keep the input order.
- `UpdateAsync` checks every part of a composite primary key, so an entity whose first key part holds its default value (for example `OrderId = 0`) can be updated; it still throws when every key part is default or any key part is `null`.
- Paginated repository reads (`GetListAsync(PaginationSpecification)`, its projection overload and the paginated `Any`) order by primary key when the specification has no `OrderBy`, so pages no longer overlap or skip rows; a caller-supplied `OrderBy` is left unchanged.

## EfCore.RepositoryPack 1.3.0 - 2026-09-27

- Composite primary key lookup through `GetByIdAsync(new object[] { ... })`.
- Fix: the paginated `Any` overload no longer blocks on async code.
- Fix: a unit of work created without a service provider builds its repositories on its own context.
- Filtered `Any` lookups resolve with a single query.

## 1.2.0 - 2026-09-27 (both packages)

- Adds `net9.0` and `net10.0` targets (packages target `net6.0`–`net10.0`).
- Raises EF Core and Microsoft.Extensions dependency floors to versions without known security advisories.
- `EfCore.RepositoryPack`: `AddEfCoreRepository` for per-scope `DbContext` registration of repositories and the unit of work.
- Fix: `BeginTransactionAsync` honours the isolation level and cancellation tokens are passed through.
- Fix: `Any(out entity)` no longer fails to translate or disposes the context.
- Fix: the `DatabaseOptions` unit of work receives its context and saves changes.
- Fix: updates of entities with reference-type (e.g. string) primary keys.
- Fix: assembly-scan registration registers `IBaseReadRepository` and skips abstract types.

## EfCore.RepositoryPack 1.1.5 - 2025-06-06

- Multi-targeting for supported .NET versions; write repository improvements, performance and minor bug fixes.

## 1.1.2 - 2024-12

- `EfCore.BaseRepositoryPack` 1.1.2: core abstractions and configuration options for repository implementations.
- `EfCore.RepositoryPack` 1.1.1 / 1.1.2: context-aware registration extensions.
