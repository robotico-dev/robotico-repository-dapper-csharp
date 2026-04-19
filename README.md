# Robotico.Repository.Dapper

`IRepository<TEntity, TId>`, `IAsyncRepository<TEntity, TId>`, and `IUnitOfWork` over `IDbConnection` / `IDbTransaction` (Dapper). Table and id column names are configurable; ensure they are not user-controlled to avoid SQL injection.

## Which interface?

| Host / scenario | Prefer |
|-----------------|--------|
| ASP.NET Core, workers | `IAsyncRepository<,>` |
| Legacy synchronous usage | `IRepository<,>` |

## Unit of work profile (`IUnitOfWorkCapabilities`)

| | |
|--|--|
| `UnitOfWorkCommitMode` | `DeferredUntilCommit` when `BeginTransaction()` was used; otherwise operations may autocommit per statement |
| `CommitCoordinatesDomainWrites` | yes when a transaction is active |
| `SupportsTransactions` | yes |

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![C#](https://img.shields.io/badge/C%23-12-239120?logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![GitHub Packages](https://img.shields.io/badge/GitHub%20Packages-Robotico.Repository.Dapper-blue?logo=github)](https://github.com/robotico-dev/robotico-repository-dapper-csharp/packages)

Use the same connection and transaction (from `DapperUnitOfWork.Connection` and `BeginTransaction()`) for repositories so that operations and `CommitAsync` participate in one transaction.

## License

See repository license file.
