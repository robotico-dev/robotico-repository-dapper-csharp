# Robotico.Repository.Dapper

`IRepository<TEntity, TId>` and `IUnitOfWork` over `IDbConnection` / `IDbTransaction` (Dapper). Table and id column names are configurable; ensure they are not user-controlled to avoid SQL injection.

Use the same connection and transaction (from `DapperUnitOfWork.Connection` and `BeginTransaction()`) for repositories so that operations and `CommitAsync` participate in one transaction.

## License

See repository license file.
