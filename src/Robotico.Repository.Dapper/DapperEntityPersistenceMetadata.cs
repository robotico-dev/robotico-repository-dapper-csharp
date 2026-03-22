using System.Reflection;

namespace Robotico.Repository.Dapper;

/// <summary>
/// Immutable reflection-derived INSERT/UPDATE command shapes for a given entity CLR type and logical id column name.
/// </summary>
internal sealed class DapperEntityPersistenceMetadata
{
    private readonly PropertyInfo[] _insertProperties;
    private readonly PropertyInfo[] _updateProperties;

    /// <summary>
    /// Initializes metadata for <paramref name="entityType"/> and <paramref name="idColumnName"/>.
    /// </summary>
    /// <param name="entityType">Entity CLR type.</param>
    /// <param name="idColumnName">Property name treated as the primary key (e.g. <c>Id</c>).</param>
    public DapperEntityPersistenceMetadata(Type entityType, string idColumnName)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(idColumnName);
        // Match DapperRepository historical behavior: default GetProperties() order (no sort).
        PropertyInfo[] readable = [.. entityType.GetProperties().Where(p => p.CanRead)];
        _insertProperties = readable;
        _updateProperties = [.. readable.Where(p => p.CanRead && p.CanWrite && p.Name != idColumnName && p.Name != "Id")];
    }

    /// <summary>
    /// Builds a parameterized INSERT for <paramref name="tableName"/> using bracket-quoted identifiers.
    /// </summary>
    public string BuildInsertSql(string tableName)
    {
        ArgumentNullException.ThrowIfNull(tableName);
        string columns = string.Join(", ", _insertProperties.Select(p => $"[{p.Name}]"));
        string values = string.Join(", ", _insertProperties.Select(p => "@" + p.Name));
        return $"INSERT INTO [{tableName}] ({columns}) VALUES ({values})";
    }

    /// <summary>
    /// Builds a parameterized UPDATE for <paramref name="tableName"/> filtered by <paramref name="idColumnName"/>.
    /// </summary>
    public string BuildUpdateSql(string tableName, string idColumnName)
    {
        ArgumentNullException.ThrowIfNull(tableName);
        ArgumentNullException.ThrowIfNull(idColumnName);
        string setClause = _updateProperties.Length > 0
            ? string.Join(", ", _updateProperties.Select(p => $"[{p.Name}] = @{p.Name}"))
            : $"[{idColumnName}] = @Id";
        return $"UPDATE [{tableName}] SET {setClause} WHERE [{idColumnName}] = @Id";
    }
}
