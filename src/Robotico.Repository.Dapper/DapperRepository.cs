using System.Data;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using Robotico.Domain;
using Robotico.Result.Errors;

namespace Robotico.Repository.Dapper;

/// <summary>
/// Dapper implementation of <see cref="IRepository{TEntity, TId}"/> using <see cref="IDbConnection"/> and optional <see cref="IDbTransaction"/>.
/// </summary>
/// <remarks>
/// <para>When using with <see cref="DapperUnitOfWork"/>, pass the unit of work's connection and transaction so that operations participate in the same transaction. Table and key column names default to <typeparamref name="TEntity"/> type name and <c>Id</c>; override via <paramref name="tableName"/> and <paramref name="idColumnName"/> if needed.</para>
/// <para>INSERT/UPDATE shapes are derived once per (entity type, <paramref name="idColumnName"/>) via <see cref="DapperEntityPersistenceMetadataCache"/>.</para>
/// <para>Ensure <paramref name="tableName"/> and <paramref name="idColumnName"/> are not user-controlled to avoid SQL injection; values are interpolated into SQL. Query parameters (e.g. entity Id) are always parameterized.</para>
/// </remarks>
/// <typeparam name="TEntity">The entity type (must implement <see cref="IEntity{TId}"/> and be a reference type).</typeparam>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Dapper maps any database/runtime failure to Result.Error(ExceptionError); duplicate key is classified separately on Add.")]
public sealed class DapperRepository<TEntity, TId>(IDbConnection connection, IDbTransaction? transaction = null, string? tableName = null, string idColumnName = "Id") : Robotico.Repository.IRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    private readonly string _tableName = tableName ?? typeof(TEntity).Name;
    private readonly DapperEntityPersistenceMetadata _persistenceMetadata = DapperEntityPersistenceMetadataCache.Get<TEntity>(idColumnName);

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null.</exception>
    public Robotico.Result.Result<TEntity> GetById(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        try
        {
            string sql = $"SELECT * FROM [{_tableName}] WHERE [{idColumnName}] = @Id";
            TEntity? entity = connection.QueryFirstOrDefault<TEntity>(sql, new { Id = id }, transaction);
            return entity is null
                ? Robotico.Result.Result.Error<TEntity>(new SimpleError($"Entity with id '{id}' not found.", "NOT_FOUND"))
                : Robotico.Result.Result.Success(entity);
        }
        catch (Exception ex)
        {
            return Robotico.Result.Result.Error<TEntity>(new ExceptionError(ex));
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
    public Robotico.Result.Result Add(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        try
        {
            string sql = _persistenceMetadata.BuildInsertSql(_tableName);
            connection.Execute(sql, entity, transaction);
            return Robotico.Result.Result.Success();
        }
        catch (Exception ex) when (DapperDuplicateKeyExceptionInspector.IsDuplicateKey(ex))
        {
            return Robotico.Result.Result.Error(new SimpleError($"Entity with id '{entity.Id}' already exists.", "DUPLICATE"));
        }
        catch (Exception ex)
        {
            return Robotico.Result.Result.Error(new ExceptionError(ex));
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
    public Robotico.Result.Result Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        try
        {
            string sql = _persistenceMetadata.BuildUpdateSql(_tableName, idColumnName);
            int rows = connection.Execute(sql, entity, transaction);
            return rows > 0
                ? Robotico.Result.Result.Success()
                : Robotico.Result.Result.Error(new SimpleError($"Entity with id '{entity.Id}' not found.", "NOT_FOUND"));
        }
        catch (Exception ex)
        {
            return Robotico.Result.Result.Error(new ExceptionError(ex));
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is null.</exception>
    public Robotico.Result.Result Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        try
        {
            string sql = $"DELETE FROM [{_tableName}] WHERE [{idColumnName}] = @Id";
            int rows = connection.Execute(sql, new { entity.Id }, transaction);
            return rows > 0
                ? Robotico.Result.Result.Success()
                : Robotico.Result.Result.Error(new SimpleError($"Entity with id '{entity.Id}' not found.", "NOT_FOUND"));
        }
        catch (Exception ex)
        {
            return Robotico.Result.Result.Error(new ExceptionError(ex));
        }
    }
}
