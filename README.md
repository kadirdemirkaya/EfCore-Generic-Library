# EfCore.RepositoryPack

[![NuGet](https://img.shields.io/nuget/v/EfCore.RepositoryPack.svg)](https://www.nuget.org/packages/EfCore.RepositoryPack)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)

Generic read/write repositories, unit of work, specifications and pagination for Entity Framework Core — works with any relational provider.

```bash
dotnet add package EfCore.RepositoryPack
```

```csharp
services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));
services.EfCoreRepositoryServiceRegistration<IBaseEntity, AppDbContext>(ServiceLifetime.Scoped, typeof(Person).Assembly);

public class PersonService(IUnitOfWork<Person> unitOfWork)
{
    public async Task<List<Person>> AdultsAsync()
        => await unitOfWork.GetReadRepository().GetListAsync(p => p.Age >= 18, asNoTracking: true);
}
```

## Features

- Generic read and write repositories with filtering, includes, tracking control and projection
- Lookup by primary key, including composite keys (`GetByIdAsync(new object[] { ... })`)
- Unit of work per entity with access to every repository flavour
- Entity-independent unit of work: repositories of several entities on one context, one save or one transaction (retry-strategy aware)
- Updates without an immediate save (`Update`), written together with other changes by one `SaveChangesAsync`
- Set-based updates and deletes by filter or specification (`UpdateWhereAsync`, `DeleteWhereAsync`) in one SQL command
- Specifications: conditions, includes, ordering, skip/take and no-tracking in one object, with opt-in split queries, ignoring global query filters and no-tracking with identity resolution
- Fluent specification builder (`Where`, `Include`, `OrderByAscending`/`OrderByDescending`, `ThenBy`, `Skip`/`Take`, `Page`, ...) and `And`/`Or`/`Not` for combining conditions into one translatable expression
- Streaming reads (`StreamAsync`) that return rows one at a time as `IAsyncEnumerable<T>` instead of a list
- Pagination with `PaginatedList<T>` (total items, total pages, page index/size, previous/next page flags)
- Keyset (cursor) pagination by a single key (`GetPageAfterAsync`) that seeks instead of skipping and does not shift when rows are inserted
- Raw SQL queries and commands with parameters (interpolated or placeholder style), transactions with isolation level
- Dependency injection registration per entity or by assembly scan
- Targets .NET 8, .NET 9 and .NET 10

## Registration

Register every entity implementing a marker type found in the given assemblies. The `DbContext` is resolved from the container, so the configuration from `AddDbContext` applies:

```csharp
services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
services.EfCoreRepositoryServiceRegistration<IBaseEntity, AppDbContext>(ServiceLifetime.Scoped, typeof(IBaseEntity).Assembly);
```

Register a single entity with a context resolved from the container. Each scope gets its own context, configured by `AddDbContext` and shared by the entity's repositories and unit of work:

```csharp
services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));
services.AddEfCoreRepository<Person, AppDbContext>();
```

Register a single entity with one context created through the parameterless constructor of `AppDbContext` (configured in `OnConfiguring`) and shared by every resolve and by every entity registered this way with the same context type:

```csharp
services.EfCoreRepositoryServiceRegistration<Person, AppDbContext>(ServiceLifetime.Scoped);
```

Register a single entity with an existing context through `DatabaseOptions`:

```csharp
services.EfCoreRepositoryServiceRegistration<Person, AppDbContext>(
    ServiceLifetime.Scoped,
    new DatabaseOptions { Connection = appDbContext });
```

## Reading

```csharp
IReadRepository<Person> read = unitOfWork.GetReadRepository();

Person person = await read.GetByIdAsync(7);
OrderLine line = await orderLines.GetByIdAsync(new object[] { orderId, lineNumber });
Person first = await read.GetAsync(p => p.Name == "Ada", q => q.Include(p => p.Baskets));
List<Person> list = await read.GetListAsync(p => p.Age > 30, asNoTracking: true);
int count = await read.CountAsync(p => p.Age > 30);
long total = await read.LongCountAsync();                 // extension, namespace EfCore.Repository.Abstractions
long adults = await read.LongCountAsync(p => p.Age >= 18);
bool any = await read.AnyAsync(p => p.Age > 30);

List<PersonDto> dtos = await read.GetListAsync(
    p => p.Age > 30,
    p => new PersonDto { Id = p.Id, Name = p.Name });
```

## Specifications and pagination

```csharp
var specification = new Specification<Person>
{
    Skip = 0,
    Take = 10,
    AsNoTracking = true,
    Includes = q => q.Include(p => p.Baskets),
    OrderBy = q => q.OrderByDescending(p => p.Age)
};
specification.Conditions.Add(p => p.Age < 100);

List<Person> people = await read.GetListAsync(specification);

var page = new PaginationSpecification<Person>
{
    PageIndex = 2,
    PageSize = 20,
    OrderBy = q => q.OrderBy(p => p.Name)
};

PaginatedList<Person> result = await read.GetListAsync(page);
// result.Items, result.TotalItems, result.TotalPages, result.PageIndex, result.PageSize,
// result.HasPreviousPage, result.HasNextPage
```

When a `PaginationSpecification` has no `OrderBy`, the repository orders the page by primary key (every key part for composite keys), so pages are stable; an `OrderBy` you set is used as given.

Any `IQueryable<T>` can be paged directly with `ToPaginatedListAsync(pageIndex, pageSize)`; it keeps the order of the query, so order it before paging.

### Fluent builder and combined conditions

The extension methods in `SpecificationBuilderExtensions` (namespace `EfCore.Repository`) configure a `Specification<T>` or `PaginationSpecification<T>` and return the same instance, so calls chain. The result is the same specification you would get by setting the properties, and both styles can be mixed:

```csharp
var specification = new Specification<Person>()
    .Where(p => p.Age < 100)
    .Include(p => p.Baskets)                                   // or .Include(q => q.Include(...).ThenInclude(...))
    .OrderByDescending(p => p.Age)
    .ThenBy(p => p.Name)
    .AsNoTracking()
    .Skip(0)
    .Take(10);

var page = new PaginationSpecification<Person>()
    .Where(p => p.IsActive)
    .OrderByAscending(p => p.Name)
    .Page(pageIndex: 2, pageSize: 20);
```

- `Where` adds a condition; several calls are combined with AND. `Include` adds to includes already set.
- `OrderByAscending` / `OrderByDescending` replace any order already set (the ascending method is not called `OrderBy` because the specification already has an `OrderBy` property); `ThenBy` / `ThenByDescending` add the next key and throw `InvalidOperationException` when no order is set yet.
- `AsNoTracking()`, `AsNoTrackingWithIdentityResolution()`, `AsSplitQuery()` and `IgnoreQueryFilters()` switch on the matching query option. `Skip`/`Take` reject negative values and `Page` values below 1 with `ArgumentOutOfRangeException`.

For OR and NOT, combine conditions with `And`, `Or` and `Not` (`PredicateExtensions`, namespace `EfCore.Repository.Expressions`) and pass the result to `Where` or to any repository filter. The parameters are rebound into one lambda, so the database provider translates it to SQL `AND` / `OR` / `NOT`:

```csharp
Expression<Func<Person, bool>> young = p => p.Age < 30;
Expression<Func<Person, bool>> senior = p => p.Age > 65;
Expression<Func<Person, bool>> blocked = p => p.IsBlocked;

var specification = new Specification<Person>().Where(young.Or(senior).And(blocked.Not()));
List<Person> people = await read.GetListAsync(young.Or(senior));
```

### Keyset (cursor) pagination

Page numbers make the database skip every earlier row, which gets slower on deep pages, and rows inserted or deleted while a client pages shift the following pages. `GetPageAfterAsync` (extension methods on `IReadRepository<TEntity>`, namespace `EfCore.Repository.Abstractions`) orders by one key and continues right after the last key the client saw:

```csharp
KeysetPage<Order, int> page = await read.GetPageAfterAsync(o => o.Id, after: null, pageSize: 50);

while (page.HasNextPage)
{
    page = await read.GetPageAfterAsync(o => o.Id, page.NextCursor, 50);
}

// newest first, only open orders
KeysetPage<Order, int> latest = await read.GetPageAfterAsync(o => o.Id, null, 20, o => o.IsOpen, descending: true, asNoTracking: true);

// by a string key, with a specification (conditions, includes and query options apply)
var spec = new SpecificationBase<Customer> { AsNoTracking = true, Includes = q => q.Include(c => c.Orders) };
KeysetPage<Customer, string> customers = await read.GetPageAfterAsync(c => c.Code, lastCode, 20, spec);
```

- `KeysetPage<TEntity, TKey>` holds `Items`, `PageSize`, `HasNextPage` and `NextCursor` (the key of the last row; pass it as `after` for the next page). Pass `null` as `after` for the first page.
- The key must be a single, unique, non-null column such as the primary key or a unique code, of a type implementing `IComparable<TKey>` that the database provider can compare and order (for example `int`, `long`, `DateTime` or `string`). With duplicate key values, rows sharing the cursor's key are skipped. Composite (multi-column) keys are not supported.
- One query per page reads `pageSize + 1` rows to know whether more follow; there is no total count. Use an index on the key.
- The key decides the order, so a specification with an `OrderBy`, with `Skip`/`Take`, or a `PaginationSpecification` is rejected with `ArgumentException`.

### Query options

Every specification (`Specification`, `PaginationSpecification`, `SpecificationBase`) has three opt-in switches; all default to `false`, which keeps the plain query:

| Property | Effect |
|---|---|
| `AsSplitQuery` | Loads included collections with one SQL command per collection instead of one joined command, avoiding a row explosion with several collection includes (relational providers). Combine with a stable `OrderBy` when paging. |
| `IgnoreQueryFilters` | Skips the model's global query filters (for example soft delete), so hidden rows are returned and, in paginated reads, counted in `TotalItems`. |
| `AsNoTrackingWithIdentityResolution` | Returns untracked entities, but a row that appears several times (for example the same parent included by several children) is one shared instance. Wins over `AsNoTracking` when both are set. |

```csharp
var orders = new Specification<Order>
{
    AsSplitQuery = true,
    AsNoTrackingWithIdentityResolution = true,
    Includes = q => q.Include(o => o.Lines).Include(o => o.Payments),
    OrderBy = q => q.OrderBy(o => o.Id)
};

var archived = new PaginationSpecification<Person> { PageIndex = 1, PageSize = 20, IgnoreQueryFilters = true };
```

### Streaming

`StreamAsync` (extension methods on `IReadRepository<TEntity>`, namespace `EfCore.Repository.Abstractions`, so they also work on the repositories a unit of work returns) returns an `IAsyncEnumerable<TEntity>` that reads the rows one at a time instead of loading a list. It accepts a `Specification<TEntity>` (conditions, includes, ordering, skip/take and query options apply as in `GetListAsync`) or a filter with an optional `asNoTracking`.

```csharp
var specification = new Specification<Person> { AsNoTracking = true, OrderBy = q => q.OrderBy(p => p.Id) };

await foreach (Person person in read.StreamAsync(specification, cancellationToken))
{
    // process one row at a time
}

await foreach (Person person in read.StreamAsync(p => p.Age > 30, asNoTracking: true))
{
}
```

The query runs when enumeration starts and holds the context's connection until it ends, so do not run other queries on the same context inside the loop. Use `AsNoTracking` for large streams (tracked rows accumulate in the change tracker), and `OrderBy` when the order matters. Cancelling the token (or one passed with `WithCancellation`) stops the enumeration with an `OperationCanceledException`.

## Writing

```csharp
IWriteRepository<Person> write = unitOfWork.GetWriteRepository();

await write.AddAsync(new Person { Name = "Ada", Age = 36 });
await write.SaveChangesAsync();

object[] key = await write.InsertAsync(new Person { Name = "Alan", Age = 41 });
await write.UpdateAsync(person);
await write.DeleteAsync(person);
```

`UpdateAsync` saves immediately. To batch updates with other changes, `Update(entity)` and `Update(entities)` mark entities as modified without saving; the next `SaveChangesAsync` (of the repository or of a unit of work on the same context) writes everything at once. They are instance methods of `WriteRepository<TEntity>` and extension methods of `IWriteRepository<TEntity>` (namespace `EfCore.Repository.Abstractions`), so they also work on the repositories a unit of work returns. An untracked entity gets the same primary key check as `UpdateAsync` (`InvalidOperationException` when a key part is `null` or every part is default); a list is checked completely before anything is attached. The key values are read through the EF Core model, so keys mapped to a field or to a shadow property are checked too; for a shadow key, set its value with `context.Entry(entity).Property("Id").CurrentValue = id` before calling `UpdateAsync` or `Update`.

```csharp
write.Update(person);
write.Update(otherPeople);
await write.SaveChangesAsync();
```

### Set-based updates and deletes

`DeleteWhereAsync` and `UpdateWhereAsync` change every matching row with one SQL command, without loading the entities, and return the number of affected rows. They are extension methods of `IWriteRepository<TEntity>` (namespace `EfCore.Repository.Abstractions`), so they work on any write repository, including those a unit of work returns.

```csharp
int deleted = await write.DeleteWhereAsync(p => p.Age > 90);
int updated = await write.UpdateWhereAsync(
    p => p.Age < 18,
    s => s.SetProperty(p => p.Name, "minor").SetProperty(p => p.Age, p => p.Age + 1));

var adults = new SpecificationBase<Person> { Conditions = { p => p.Age >= 18, p => p.Name != "" } };
int touched = await write.UpdateWhereAsync(adults, s => s.SetProperty(p => p.Age, p => p.Age + 1));
```

- **They bypass the change tracker.** The command runs immediately — no `SaveChangesAsync` needed. Pending tracked changes are not saved or discarded, and entities already tracked by the context keep their old values (or stay tracked although their row is deleted); reload them if you keep using them.
- **Global query filters apply.** Rows hidden by a query filter of the entity (for example soft-deleted rows) are not updated or deleted.
- **They join the current transaction.** Inside `BeginTransactionAsync` or `IUnitOfWork.ExecuteInTransactionAsync` the command is part of that transaction, so a rollback undoes it.
- The setters lambda is passed unchanged to EF Core's set-based update. On .NET 10 (EF Core 10) the parameter is `Action<UpdateSettersBuilder<TEntity>>`, on .NET 8 and 9 it is `Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>>`; an inline lambda such as the ones above compiles unchanged on every target.
- The specification overloads use only `Conditions` (all must match); includes, ordering and no-tracking are ignored. A specification without conditions, with a `null` condition, with `Skip`/`Take`, or a `PaginationSpecification` throws `ArgumentException` instead of touching more rows than intended — pass `_ => true` to the filter overload to target every row on purpose.

### Concurrency

For entities with a concurrency token (`[ConcurrencyCheck]`, `[Timestamp]` or `IsConcurrencyToken()`/`IsRowVersion()`), a save that finds the row changed by someone else throws `DbUpdateConcurrencyException` from `SaveChangesAsync`, `UpdateAsync`, `DeleteAsync` or the unit of work save. The repositories pass it through unchanged and do not retry or resolve the conflict: catch it, reload or merge the values, and save again.

## Transactions and raw SQL

```csharp
await using var transaction = await write.BeginTransactionAsync(IsolationLevel.ReadCommitted);
await write.ExecuteCommandInterpolatedAsync($"UPDATE Persons SET Age = Age + 1 WHERE Id = {id}");
await write.ExecuteCommandAsync("UPDATE Persons SET Age = Age + 1 WHERE Id = {0}", id);
await transaction.CommitAsync();

List<Person> rows = await read.ExecuteQueryInterpolatedAsync($"SELECT * FROM Persons WHERE Age > {minimumAge}");
List<Person> same = await read.ExecuteQueryAsync("SELECT * FROM Persons WHERE Age > {0}", minimumAge);
```

`ExecuteQueryInterpolatedAsync` and `ExecuteCommandInterpolatedAsync` send every interpolated value as a database parameter (`null` as database `NULL`), so a value such as `x' OR '1'='1` is only ever compared or stored as text. With `ExecuteQueryAsync` and `ExecuteCommandAsync`, pass values through the `{0}`, `{1}`, ... placeholders and the parameter array; never concatenate or interpolate user input into their SQL text — an interpolated string passed to them is turned into plain text first, so its values end up inside the SQL. `ExecuteCommandAsync(string, CancellationToken)` sends the text without any parameter.

`ExecuteCommandInterpolatedAsync` returns `true` when at least one row was affected, like `ExecuteCommandAsync`. It is an instance method of the write repository classes and an extension method on `IBaseWriteRepository<TEntity>` (`WriteRepositoryExtensions`, namespace `EfCore.Repository.Abstractions`), so it also works on `IWriteRepository<TEntity>` and on the repositories returned by a unit of work. The command runs immediately, bypasses the change tracker and joins the context's current transaction, so a rollback undoes it.

## Several entities in one unit of work

`IUnitOfWork` (without a type argument) is not bound to an entity: every repository it returns works on the same context, so changes to different entities are written by one save or inside one transaction. `AddEfCoreRepository` registers it on the context resolved from the container (one per scope with `AddDbContext`); it is bound to the context type of the first `AddEfCoreRepository` call. Without the container, create it with `new UnitOfWork(dbContext)`.

```csharp
public class CheckoutService(IUnitOfWork unitOfWork)
{
    public Task CheckoutAsync(Order order, Payment payment, CancellationToken cancellationToken)
        => unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await unitOfWork.GetWriteRepository<Order>().AddAsync(order, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.GetWriteRepository<Payment>().AddAsync(payment, ct);
        }, IsolationLevel.ReadCommitted, cancellationToken);
}
```

`ExecuteInTransactionAsync` runs the delegate through the provider's execution strategy (`Database.CreateExecutionStrategy()`), so a configured retry strategy re-runs the whole delegate; keep it repeatable. Pending changes are saved before the commit. If the delegate, the save or the commit fails, the transaction is rolled back, the change tracker is cleared and the exception is rethrown — nothing of the attempt is written, now or by a later save. Inside an already open transaction the delegate joins it and the owner of that transaction commits or rolls back.

`SaveChangesAsync(CancellationToken)` is available on `IUnitOfWork`, on `UnitOfWork<TEntity>` and on `BaseWriteRepository<TEntity>` (the `IWriteRepository<TEntity>` method already takes a token).

## Packages

Both packages share one version number.

| Package | Version | Purpose |
|---|---|---|
| [EfCore.RepositoryPack](https://www.nuget.org/packages/EfCore.RepositoryPack) | 2.1.0 | Repositories, unit of work, specifications, registration |
| [EfCore.BaseRepositoryPack](https://www.nuget.org/packages/EfCore.BaseRepositoryPack) | 2.1.0 | Base abstractions and options, installed automatically |

## Upgrading from 1.x

2.0.0 targets .NET 8, .NET 9 and .NET 10 and removes a few members:

- `ExecuteQueryAsync(string)` without parameters → `ExecuteQueryInterpolatedAsync($"...")` or `ExecuteQueryAsync(sql, parameters)`
- the public `_dbContext` members of the repositories, `Table` and `UnitOfWork` → the repository's `Table` property, or `unitOfWork.GetTable().Table`
- `DatabaseOptions.RetryCount` / `IsRetry`, `ParameterValidationAttribute` and `ConfigurationHelpers.GetOptions(IServiceCollection, string)` → removed; use `configuration.GetOptions<T>(section)`

The full list is in the [changelog](https://github.com/kadirdemirkaya/EfCore-Generic-Library/blob/main/CHANGELOG.md).

## License

[MIT](https://opensource.org/licenses/MIT) · [Source](https://github.com/kadirdemirkaya/EfCore-Generic-Library)
