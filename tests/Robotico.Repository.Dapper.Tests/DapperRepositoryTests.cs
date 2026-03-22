using Microsoft.Data.Sqlite;
using Robotico.Result.Errors;
using Xunit;

namespace Robotico.Repository.Dapper.Tests;

/// <summary>
/// Tests for DapperRepository and DapperUnitOfWork: success, NOT_FOUND, DUPLICATE, null guards, transaction, law.
/// </summary>
public sealed class DapperRepositoryTests
{
    [Fact]
    public async Task Add_then_GetById_yields_same_entity()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        DapperRepositoryTestsSqlite.CreateSampleEntityTable(connection);
        DapperUnitOfWork uow = new(connection);
        uow.BeginTransaction();
        DapperRepository<SampleEntity, Guid> repo = new(connection, uow.Transaction, "SampleEntity", "Id");

        SampleEntity entity = new() { Id = Guid.NewGuid() };
        Robotico.Result.Result addResult = repo.Add(entity);
        Assert.True(addResult.IsSuccess());

        Robotico.Result.Result commitResult = await uow.CommitAsync();
        Assert.True(commitResult.IsSuccess());

        DapperRepository<SampleEntity, Guid> repoRead = new(connection, null, "SampleEntity", "Id");
        Robotico.Result.Result<SampleEntity> getResult = repoRead.GetById(entity.Id);
        Assert.True(getResult.IsSuccess(out SampleEntity? loaded));
        Assert.Equal(entity.Id, loaded!.Id);
    }

    [Fact]
    public void GetById_returns_NOT_FOUND_when_not_added()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        DapperRepositoryTestsSqlite.CreateSampleEntityTable(connection);
        DapperRepository<SampleEntity, Guid> repo = new(connection, null, "SampleEntity", "Id");

        Robotico.Result.Result<SampleEntity> result = repo.GetById(Guid.NewGuid());

        Assert.True(result.IsError(out IError? err));
        Assert.Equal("NOT_FOUND", err!.Code);
    }

    [Fact]
    public void Add_returns_DUPLICATE_when_id_exists()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        DapperRepositoryTestsSqlite.CreateSampleEntityTable(connection);
        DapperRepository<SampleEntity, Guid> repo = new(connection, null, "SampleEntity", "Id");
        Guid id = Guid.NewGuid();
        repo.Add(new() { Id = id });

        Robotico.Result.Result result = repo.Add(new() { Id = id });

        Assert.True(result.IsError(out IError? err));
        Assert.Equal("DUPLICATE", err!.Code);
    }

    [Fact]
    public void GetById_throws_ArgumentNullException_when_id_is_null()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE SampleEntityWithStringId (Id TEXT PRIMARY KEY)";
        cmd.ExecuteNonQuery();
        DapperRepository<SampleEntityWithStringId, string> repo = new(connection, null, "SampleEntityWithStringId", "Id");

        Assert.Throws<ArgumentNullException>(() => repo.GetById(null!));
    }

    [Fact]
    public void Add_throws_ArgumentNullException_when_entity_is_null()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        DapperRepositoryTestsSqlite.CreateSampleEntityTable(connection);
        DapperRepository<SampleEntity, Guid> repo = new(connection, null, "SampleEntity", "Id");

        Assert.Throws<ArgumentNullException>(() => repo.Add(null!));
    }

    [Fact]
    public void DapperUnitOfWork_throws_ArgumentNullException_when_connection_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => new DapperUnitOfWork(null!));
    }

    [Fact]
    public async Task CommitAsync_returns_success_when_no_transaction()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        DapperUnitOfWork uow = new(connection);
        Robotico.Result.Result result = await uow.CommitAsync();
        Assert.True(result.IsSuccess());
    }
}
