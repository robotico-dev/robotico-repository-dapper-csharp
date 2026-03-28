namespace System.Data.SqlClient;

/// <summary>Shape-only stand-in for legacy System.Data.SqlClient.SqlException.Number checks.</summary>
public sealed class SqlException : Exception
{
    public SqlException()
    {
    }

    public SqlException(string message) : base(message)
    {
    }

    public SqlException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public object? Number { get; set; }
}
