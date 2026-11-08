# Changelog

The repository ships two packages with independent versions: `EfCore.RepositoryPack` (`src/EfCore.Repository`)
and `EfCore.BaseRepositoryPack` (`src/Base.Repository`). The release number and the NuGet publish are chosen by
the repository owner.

## Unreleased

- Added `LICENSE.txt` and this changelog.

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
