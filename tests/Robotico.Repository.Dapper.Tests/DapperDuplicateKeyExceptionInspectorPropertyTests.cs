using System.Globalization;
using CsCheck;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Robotico.Repository.Dapper.Tests;

/// <summary>
/// Property-based checks for <see cref="DapperDuplicateKeyExceptionInspector"/>.
/// </summary>
public sealed class DapperDuplicateKeyExceptionInspectorPropertyTests
{
    [Fact]
    public void IsDuplicateKey_true_for_sqlite_error_19_with_unique_in_message_for_any_int_suffix()
    {
        Gen.Int.Sample(i =>
        {
            string suffix = i.ToString(CultureInfo.InvariantCulture);
            SqliteException inner = new($"SQLite Error 19: 'UNIQUE constraint failed: t.{suffix}'.", 19);
            return DapperDuplicateKeyExceptionInspector.IsDuplicateKey(inner);
        });
    }
}
