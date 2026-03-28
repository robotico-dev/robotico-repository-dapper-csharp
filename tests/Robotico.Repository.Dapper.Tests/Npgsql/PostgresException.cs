// Test-only type: FullName must match Npgsql.PostgresException for DapperDuplicateKeyExceptionInspector reflection.

namespace Npgsql;

/// <summary>Shape-only stand-in for Npgsql.PostgresException.SqlState checks.</summary>
public sealed class PostgresException : Exception
{
    public PostgresException()
    {
    }

    public PostgresException(string message) : base(message)
    {
    }

    public PostgresException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public string? SqlState { get; set; }
}
