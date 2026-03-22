using Robotico.Domain;

namespace Robotico.Repository.Dapper.Tests;

/// <summary>
/// Sample entity with string id for null-guard tests.
/// </summary>
public sealed class SampleEntityWithStringId : IEntity<string>
{
    /// <inheritdoc />
    public string Id { get; init; } = string.Empty;
}
