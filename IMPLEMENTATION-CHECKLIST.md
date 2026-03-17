# Implementation checklist (10/10 quality)

Quality bar: [ARCHITECT-RATINGS-CSHARP-IMPLEMENTATIONS.adoc](../../docs/ARCHITECT-RATINGS-CSHARP-IMPLEMENTATIONS.adoc). Align with **robotico-results-csharp** and **robotico-repository-inmemory-csharp**.

- [ ] Implement `DapperRepository<TEntity, TId>` : `IRepository<TEntity, TId>` using `IDbConnection` and optional `IDbTransaction` from UoW.
- [ ] Implement `DapperUnitOfWork` : `IUnitOfWork`; provide connection and transaction; `CommitAsync` → commit transaction.
- [ ] Map SQL exceptions and "no rows" to `Result.Error` (NOT_FOUND, DUPLICATE, etc.).
- [ ] XML docs, guards, tests (e.g. SQLite in-memory).

Reference: `Robotico.Repository.InMemory` in robotico-repository-inmemory-csharp.
