using BenchmarkDotNet.Attributes;
using Robotico.Repository.Dapper;

namespace Robotico.Repository.Dapper.Benchmarks;

/// <summary>
/// Warm-path cost of <see cref="DapperEntityPersistenceMetadataCache"/> after first reflection.
/// </summary>
[MemoryDiagnoser]
public sealed class DapperMetadataCacheBenchmarks
{
    private const string IdColumn = "Id";

    /// <summary>
    /// After BenchmarkDotNet warmup, exercises the concurrent dictionary hot path (return type is public void for API accessibility).
    /// </summary>
    [Benchmark]
    public void Cache_get_after_warmup()
    {
        DapperEntityPersistenceMetadata metadata = DapperEntityPersistenceMetadataCache.Get<BenchmarkEntity>(IdColumn);
        _ = metadata;
    }
}
