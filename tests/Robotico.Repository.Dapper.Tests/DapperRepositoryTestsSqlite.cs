using Dapper;
using Microsoft.Data.Sqlite;

namespace Robotico.Repository.Dapper.Tests;

/// <summary>
/// Shared SQLite setup for <see cref="DapperRepositoryTests"/>.
/// </summary>
internal static class DapperRepositoryTestsSqlite
{
    private static readonly object HandlerRegisterGate = new();
    private static bool s_sqliteGuidHandlerRegistered;

    /// <summary>
    /// Opens an in-memory SQLite connection.
    /// </summary>
    internal static SqliteConnection CreateConnection()
    {
        EnsureSqliteGuidTypeHandlerRegistered();
        SqliteConnection connection = new("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static void EnsureSqliteGuidTypeHandlerRegistered()
    {
        if (s_sqliteGuidHandlerRegistered)
        {
            return;
        }

        lock (HandlerRegisterGate)
        {
            if (s_sqliteGuidHandlerRegistered)
            {
                return;
            }

            SqlMapper.AddTypeHandler(new SqliteStringGuidTypeHandler());
            s_sqliteGuidHandlerRegistered = true;
        }
    }

    /// <summary>
    /// Creates the <c>SampleEntity</c> table used by tests.
    /// </summary>
    internal static void CreateSampleEntityTable(SqliteConnection connection)
    {
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE SampleEntity (Id TEXT PRIMARY KEY)";
        cmd.ExecuteNonQuery();
    }
}
