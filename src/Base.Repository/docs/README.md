# EfCore.BaseRepositoryPack

This is the base package for [`EfCore.RepositoryPack`](https://www.nuget.org/packages/EfCore.RepositoryPack).  
It contains shared abstractions, base repository classes, and helper components used by the main library.

> This package is **not intended for direct use !**.  
> You should reference and use [`EfCore.RepositoryPack`](https://www.nuget.org/packages/EfCore.RepositoryPack) in your application.

---

## Installation

```bash
dotnet add package EfCore.BaseRepositoryPack
```

## Versions and target frameworks

- Version 2.1.0, shared with [`EfCore.RepositoryPack`](https://www.nuget.org/packages/EfCore.RepositoryPack): both packages always carry the same version number.
- Targets .NET 8, .NET 9 and .NET 10.
- 2.0.0 removes `IBaseReadRepository.ExecuteQueryAsync(string)` (use `ExecuteQueryInterpolatedAsync($"...")`, which sends interpolated values as database parameters, or `ExecuteQueryAsync(sql, parameters)`), `DatabaseOptions.RetryCount` / `IsRetry` and `ConfigurationHelpers.GetOptions(IServiceCollection, string)` (use `configuration.GetOptions<T>(section)`). See the [changelog](https://github.com/kadirdemirkaya/EfCore-Generic-Library/blob/main/CHANGELOG.md).
