using System.Data;
using Dapper;
using Robotico.Domain;
using Robotico.Repository;
using Robotico.Result;
using Robotico.Result.Errors;

namespace Robotico.Repository.Dapper;

/// <summary>
/// Dapper implementation of <see cref="IRepository{TEntity, TId}"/> using <see cref="IDbConnection"/> and optional <see cref="IDbTransaction"/>.
/// </summary>
/// <remarks>
/// <para>When using with <see cref="DapperUnitOfWork"/>, pass the unit of work's connection and transaction so that operations participate in the same transaction. Table and key column names default to <typeparamref name="TEntity"/> type name and <c>Id</c>; override via <paramref name="tableName"/> and <paramref name="idColumnName"/> if needed.</para>
/// <para>Ensure <paramref name="tableName"/> and <paramref name="idColumnName"/> are not user-controlled to avoid SQL injection; values are interpolated into SQL. Query parameters (e.g. entity Id) are always parameterized.</para>
/// </remarks>
/// <typeparam name="TEntity">The entity type (must implement <see cref="IEntity{TId}"/> and be a reference type).</typeparam>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public sealed class DapperRepository<TEntity, TId>(
    IDbConnection connection,
    IDbTransaction? transaction = null,
    string? tableName = null,
    string idColumnName = "Id") : IRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    private readonly string _tableName = tableName ?? typeof(TEntity).Name;

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
            var props = typeof(TEntity).GetProperties().Where(p => p.CanRead).ToList();
            string columns = string.Join(", ", props.Select(p => $"[{p.Name}]"));
            string values = string.Join(", ", props.Select(p => "@" + p.Name));
            string sql = $"INSERT INTO [{_tableName}] ({columns}) VALUES ({values})";
            connection.Execute(sql, entity, transaction);
            return Robotico.Result.Result.Success();
        }
        catch (Exception ex) when (IsDuplicateKey(ex))
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
            var props = typeof(TEntity).GetProperties()
                .Where(p => p.CanRead && p.CanWrite && p.Name != idColumnName && p.Name != "Id")
                .ToList();
            string setClause = props.Count > 0
                ? string.Join(", ", props.Select(p => $"[{p.Name}] = @{p.Name}"))
                : $"[{idColumnName}] = @Id";

            string sql = $"UPDATE [{_tableName}] SET {setClause} WHERE [{idColumnName}] = @Id";
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

    private static bool IsDuplicateKey(Exception ex)
    {
        string msg = ex.Message ?? "";
        return msg.Contains("unique", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase);
    }
}
