using Robotico.Domain;

namespace Robotico.Repository.Dapper.Tests;

/// <summary>Entity with an updatable non-id property for UPDATE SQL coverage.</summary>
public sealed class SampleEntityWithName : IEntity<Guid>
{
    public Guid Id { get; init; }

    public string Name { get; set; } = "";
}
