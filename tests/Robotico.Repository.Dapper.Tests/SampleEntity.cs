using Robotico.Domain;

namespace Robotico.Repository.Dapper.Tests;

/// <summary>
/// Sample entity for DapperRepository tests.
/// </summary>
public sealed class SampleEntity : IEntity<Guid>
{
    /// <inheritdoc />
    public Guid Id { get; init; }
}
