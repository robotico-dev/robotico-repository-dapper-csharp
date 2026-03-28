using Microsoft.Data.Sqlite;
using Xunit;

namespace Robotico.Repository.Dapper.Tests;

/// <summary>Exercises provider-specific branches in <see cref="DapperDuplicateKeyExceptionInspector"/> that message heuristics skip.</summary>
public sealed class DapperDuplicateKeyInspectorProviderBranchTests
{
    [Fact]
    public void IsDuplicateKey_true_when_sqlite_extended_error_2067_without_keyword_message()
    {
        SqliteException inner = new(string.Empty, 19, 2067);
        Assert.True(DapperDuplicateKeyExceptionInspector.IsDuplicateKey(new InvalidOperationException("batch", inner)));
    }

    [Fact]
    public void IsDuplicateKey_true_when_npgsql_sqlstate_is_23505()
    {
        Npgsql.PostgresException inner = new() { SqlState = "23505" };
        Assert.True(DapperDuplicateKeyExceptionInspector.IsDuplicateKey(new InvalidOperationException("batch", inner)));
    }

    [Fact]
    public void IsDuplicateKey_false_when_npgsql_sqlstate_is_not_unique_violation()
    {
        Npgsql.PostgresException inner = new() { SqlState = "42P01" };
        Assert.False(DapperDuplicateKeyExceptionInspector.IsDuplicateKey(new InvalidOperationException("batch", inner)));
    }

    [Fact]
    public void IsDuplicateKey_true_when_microsoft_sqlclient_number_is_2627()
    {
        Microsoft.Data.SqlClient.SqlException inner = new() { Number = 2627 };
        Assert.True(DapperDuplicateKeyExceptionInspector.IsDuplicateKey(new InvalidOperationException("batch", inner)));
    }

    [Fact]
    public void IsDuplicateKey_true_when_microsoft_sqlclient_number_is_2601()
    {
        Microsoft.Data.SqlClient.SqlException inner = new() { Number = 2601 };
        Assert.True(DapperDuplicateKeyExceptionInspector.IsDuplicateKey(new InvalidOperationException("batch", inner)));
    }

    [Fact]
    public void IsDuplicateKey_true_when_legacy_sqlclient_number_is_2627()
    {
        System.Data.SqlClient.SqlException inner = new() { Number = 2627 };
        Assert.True(DapperDuplicateKeyExceptionInspector.IsDuplicateKey(new InvalidOperationException("batch", inner)));
    }

    [Fact]
    public void IsDuplicateKey_false_when_sqlclient_number_is_not_unique_violation()
    {
        Microsoft.Data.SqlClient.SqlException inner = new() { Number = 547 };
        Assert.False(DapperDuplicateKeyExceptionInspector.IsDuplicateKey(new InvalidOperationException("batch", inner)));
    }

    [Fact]
    public void IsDuplicateKey_false_when_sqlite_extended_not_unique_and_message_plain()
    {
        SqliteException inner = new("SQLite Error 19.", 19, 0);
        Assert.False(DapperDuplicateKeyExceptionInspector.IsDuplicateKey(new InvalidOperationException("batch", inner)));
    }

    [Fact]
    public void IsDuplicateKey_false_when_sqlclient_number_not_int_even_if_unique_codes()
    {
        Microsoft.Data.SqlClient.SqlException inner = new() { Number = (long)2627 };
        Assert.False(DapperDuplicateKeyExceptionInspector.IsDuplicateKey(new InvalidOperationException("batch", inner)));
    }

    [Fact]
    public void IsDuplicateKey_false_when_legacy_sqlclient_number_not_int()
    {
        System.Data.SqlClient.SqlException inner = new() { Number = (short)2627 };
        Assert.False(DapperDuplicateKeyExceptionInspector.IsDuplicateKey(new InvalidOperationException("batch", inner)));
    }
}
