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
- Specifications: conditions, includes, ordering, skip/take and no-tracking in one object
- Pagination with `PaginatedList<T>` (total items, total pages, page index/size)
- Raw SQL queries with parameters (interpolated or placeholder style) and commands, transactions with isolation level
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
// result.Items, result.TotalItems, result.TotalPages, result.PageIndex, result.PageSize
```

When a `PaginationSpecification` has no `OrderBy`, the repository orders the page by primary key (every key part for composite keys), so pages are stable; an `OrderBy` you set is used as given.

Any `IQueryable<T>` can be paged directly with `ToPaginatedListAsync(pageIndex, pageSize)`; it keeps the order of the query, so order it before paging.

## Writing

```csharp
IWriteRepository<Person> write = unitOfWork.GetWriteRepository();

await write.AddAsync(new Person { Name = "Ada", Age = 36 });
await write.SaveChangesAsync();

object[] key = await write.InsertAsync(new Person { Name = "Alan", Age = 41 });
await write.UpdateAsync(person);
await write.DeleteAsync(person);
```

## Transactions and raw SQL

```csharp
await using var transaction = await write.BeginTransactionAsync(IsolationLevel.ReadCommitted);
await write.ExecuteCommandAsync("UPDATE Persons SET Age = Age + 1 WHERE Id = {0}", id);
await transaction.CommitAsync();

List<Person> rows = await read.ExecuteQueryInterpolatedAsync($"SELECT * FROM Persons WHERE Age > {minimumAge}");
List<Person> same = await read.ExecuteQueryAsync("SELECT * FROM Persons WHERE Age > {0}", minimumAge);
```

`ExecuteQueryInterpolatedAsync` sends every interpolated value as a database parameter. With `ExecuteQueryAsync`, pass values through the `{0}`, `{1}`, ... placeholders and the parameter array; never concatenate or interpolate user input into its SQL text.

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
| [EfCore.RepositoryPack](https://www.nuget.org/packages/EfCore.RepositoryPack) | 2.0.0 | Repositories, unit of work, specifications, registration |
| [EfCore.BaseRepositoryPack](https://www.nuget.org/packages/EfCore.BaseRepositoryPack) | 2.0.0 | Base abstractions and options, installed automatically |

## Upgrading from 1.x

2.0.0 targets .NET 8, .NET 9 and .NET 10 and removes a few members:

- `ExecuteQueryAsync(string)` without parameters → `ExecuteQueryInterpolatedAsync($"...")` or `ExecuteQueryAsync(sql, parameters)`
- the public `_dbContext` members of the repositories, `Table` and `UnitOfWork` → the repository's `Table` property, or `unitOfWork.GetTable().Table`
- `DatabaseOptions.RetryCount` / `IsRetry`, `ParameterValidationAttribute` and `ConfigurationHelpers.GetOptions(IServiceCollection, string)` → removed; use `configuration.GetOptions<T>(section)`

The full list is in the [changelog](https://github.com/kadirdemirkaya/EfCore-Generic-Library/blob/main/CHANGELOG.md).

## License

[MIT](https://opensource.org/licenses/MIT) · [Source](https://github.com/kadirdemirkaya/EfCore-Generic-Library)
