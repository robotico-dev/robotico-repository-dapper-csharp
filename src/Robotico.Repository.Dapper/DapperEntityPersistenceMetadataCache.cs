using System.Collections.Concurrent;

namespace Robotico.Repository.Dapper;

/// <summary>
/// Per-(entity type, id column) cache for <see cref="DapperEntityPersistenceMetadata"/> so reflection runs once per combination.
/// </summary>
internal static class DapperEntityPersistenceMetadataCache
{
    private static readonly ConcurrentDictionary<(Type EntityType, string IdColumn), DapperEntityPersistenceMetadata> Cache = new();

    /// <summary>
    /// Gets metadata for <typeparamref name="TEntity"/> and <paramref name="idColumnName"/>.
    /// </summary>
    public static DapperEntityPersistenceMetadata Get<TEntity>(string idColumnName)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(idColumnName);
        Type entityType = typeof(TEntity);
        (Type EntityType, string IdColumn) key = (entityType, idColumnName);
        return Cache.GetOrAdd(key, static k => new DapperEntityPersistenceMetadata(k.EntityType, k.IdColumn));
    }
}
