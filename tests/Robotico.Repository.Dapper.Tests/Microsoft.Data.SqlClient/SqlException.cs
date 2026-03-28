namespace Microsoft.Data.SqlClient;

/// <summary>Shape-only stand-in for Microsoft.Data.SqlClient.SqlException.Number checks.</summary>
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

    /// <summary>Real driver exposes int; object allows tests to model non-int shapes for reflection.</summary>
    public object? Number { get; set; }
}
