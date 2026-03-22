namespace Robotico.Repository.Dapper.Benchmarks;

/// <summary>
/// Minimal entity type for metadata-cache benchmarks (not used for SQL).
/// </summary>
public sealed class BenchmarkEntity
{
    /// <summary>
    /// Sample identifier.
    /// </summary>
    public int Id { get; set; }
}
