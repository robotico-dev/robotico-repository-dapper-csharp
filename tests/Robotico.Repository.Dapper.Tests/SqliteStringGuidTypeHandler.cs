using System.Data;
using Dapper;

namespace Robotico.Repository.Dapper.Tests;

/// <summary>
/// Maps <see cref="Guid"/> to SQLite TEXT for Microsoft.Data.Sqlite + Dapper tests (column affinity TEXT).
/// </summary>
public sealed class SqliteStringGuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    /// <inheritdoc />
    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        parameter.DbType = DbType.String;
        parameter.Value = value.ToString();
    }

    /// <inheritdoc />
    public override Guid Parse(object value)
    {
        return value switch
        {
            Guid guid => guid,
            string text => Guid.Parse(text),
            byte[] bytes when bytes.Length == 16 => new Guid(bytes),
            _ => throw new InvalidOperationException($"Cannot convert {value.GetType().FullName} to Guid."),
        };
    }
}
