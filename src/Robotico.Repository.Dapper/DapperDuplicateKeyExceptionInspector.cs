using System.Globalization;
using System.Reflection;

namespace Robotico.Repository.Dapper;

/// <summary>
/// Classifies database exceptions as unique/primary-key violations for generic Dapper CRUD, combining
/// message heuristics with provider-shaped inner exceptions (without hard references on ADO.NET drivers).
/// </summary>
internal static class DapperDuplicateKeyExceptionInspector
{
    private const string PostgresExceptionTypeName = "Npgsql.PostgresException";
    private const string MsSqlExceptionTypeName = "Microsoft.Data.SqlClient.SqlException";
    private const string LegacySqlExceptionTypeName = "System.Data.SqlClient.SqlException";
    private const string SqliteExceptionTypeName = "Microsoft.Data.Sqlite.SqliteException";

    /// <summary>
    /// Returns whether <paramref name="ex"/> is treated as a unique/primary-key violation on insert.
    /// </summary>
    public static bool IsDuplicateKey(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        if (MessageIndicatesDuplicateKey(ex.Message))
        {
            return true;
        }

        for (Exception? current = ex.InnerException; current is not null; current = current.InnerException)
        {
            if (MessageIndicatesDuplicateKey(current.Message))
            {
                return true;
            }

            if (IsSqliteUniqueConstraint(current))
            {
                return true;
            }

            if (IsPostgresUniqueViolation(current))
            {
                return true;
            }

            if (IsSqlServerUniqueViolation(current))
            {
                return true;
            }

        }

        return false;
    }

    private static bool MessageIndicatesDuplicateKey(string? message)
    {
        string text = message ?? string.Empty;
        return text.Contains("unique", StringComparison.OrdinalIgnoreCase)
            || text.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            || text.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase)
            || text.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSqliteUniqueConstraint(Exception e)
    {
        Type type = e.GetType();
        if (type.FullName != SqliteExceptionTypeName)
        {
            return false;
        }

        int? extended = TryGetInt32Property(e, "SqliteExtendedErrorCode");
        if (extended == 2067)
        {
            return true;
        }

        string text = e.Message ?? string.Empty;
        if (text.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        int? code = TryGetInt32Property(e, "SqliteErrorCode");
        return code == 19 && text.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPostgresUniqueViolation(Exception e)
    {
        Type type = e.GetType();
        if (type.FullName != PostgresExceptionTypeName)
        {
            return false;
        }

        object? sqlState = type.GetProperty("SqlState")?.GetValue(e);
        return sqlState is string state && string.Equals(state, "23505", StringComparison.Ordinal);
    }

    private static bool IsSqlServerUniqueViolation(Exception e)
    {
        Type type = e.GetType();
        if (type.FullName != MsSqlExceptionTypeName && type.FullName != LegacySqlExceptionTypeName)
        {
            return false;
        }

        object? number = type.GetProperty("Number")?.GetValue(e);
        if (number is int n)
        {
            return n is 2601 or 2627;
        }

        return false;
    }

    private static int? TryGetInt32Property(object target, string propertyName)
    {
        PropertyInfo? property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        object? value = property?.GetValue(target);
        return value switch
        {
            int i => i,
            null => null,
            _ when value.GetType().IsEnum => Convert.ToInt32(value, CultureInfo.InvariantCulture),
            _ => null,
        };
    }
}
