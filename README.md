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
- Unit of work per entity with access to every repository flavour
- Specifications: conditions, includes, ordering, skip/take and no-tracking in one object
- Pagination with `PaginatedList<T>` (total items, total pages, page index/size)
- Raw SQL queries and commands, transactions
- Dependency injection registration per entity or by assembly scan
- Targets .NET 6, .NET 7, .NET 8, .NET 9 and .NET 10

## Registration

Register every entity implementing a marker type found in the given assemblies. The `DbContext` is resolved from the container, so the configuration from `AddDbContext` applies:

```csharp
services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
services.EfCoreRepositoryServiceRegistration<IBaseEntity, AppDbContext>(ServiceLifetime.Scoped, typeof(IBaseEntity).Assembly);
```

Register a single entity:

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

Any `IQueryable<T>` can be paged directly with `ToPaginatedListAsync(pageIndex, pageSize)`.

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
await using var transaction = await write.BeginTransactionAsync();
await write.ExecuteCommandAsync("UPDATE Persons SET Age = Age + 1 WHERE Id = {0}", id);
await transaction.CommitAsync();

List<Person> rows = await read.ExecuteQueryAsync("SELECT * FROM Persons WHERE Age > {0}", 30);
```

## Packages

| Package | Purpose |
|---|---|
| [EfCore.RepositoryPack](https://www.nuget.org/packages/EfCore.RepositoryPack) | Repositories, unit of work, specifications, registration |
| [EfCore.BaseRepositoryPack](https://www.nuget.org/packages/EfCore.BaseRepositoryPack) | Base abstractions and options, installed automatically |

## License

[MIT](https://opensource.org/licenses/MIT) · [Source](https://github.com/kadirdemirkaya/EfCore-Generic-Library)
