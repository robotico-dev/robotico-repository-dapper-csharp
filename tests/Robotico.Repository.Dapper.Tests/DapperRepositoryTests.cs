using System.Data;
using Microsoft.Data.Sqlite;
using Robotico.Result.Errors;
using Xunit;

namespace Robotico.Repository.Dapper.Tests;

/// <summary>
/// Tests for DapperRepository and DapperUnitOfWork: success, NOT_FOUND, DUPLICATE, null guards, transaction, law.
/// </summary>
public sealed class DapperRepositoryTests
{
    private static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static void CreateTable(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE SampleEntity (Id TEXT PRIMARY KEY)";
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void Add_then_GetById_yields_same_entity()
    {
        using var connection = CreateConnection();
        CreateTable(connection);
        var uow = new DapperUnitOfWork(connection);
        uow.BeginTransaction();
        var repo = new DapperRepository<SampleEntity, Guid>(connection, uow.Transaction, "SampleEntity", "Id");

        var entity = new SampleEntity { Id = Guid.NewGuid() };
        Robotico.Result.Result addResult = repo.Add(entity);
        Assert.True(addResult.IsSuccess());

        Robotico.Result.Result commitResult = uow.CommitAsync().GetAwaiter().GetResult();
        Assert.True(commitResult.IsSuccess());

        var repoRead = new DapperRepository<SampleEntity, Guid>(connection, null, "SampleEntity", "Id");
        Robotico.Result.Result<SampleEntity> getResult = repoRead.GetById(entity.Id);
        Assert.True(getResult.IsSuccess(out var loaded));
        Assert.Equal(entity.Id, loaded!.Id);
    }

    [Fact]
    public void GetById_returns_NOT_FOUND_when_not_added()
    {
        using var connection = CreateConnection();
        CreateTable(connection);
        var repo = new DapperRepository<SampleEntity, Guid>(connection, null, "SampleEntity", "Id");

        Robotico.Result.Result<SampleEntity> result = repo.GetById(Guid.NewGuid());

        Assert.True(result.IsError(out var err));
        Assert.Equal("NOT_FOUND", err!.Code);
    }

    [Fact]
    public void Add_returns_DUPLICATE_when_id_exists()
    {
        using var connection = CreateConnection();
        CreateTable(connection);
        var repo = new DapperRepository<SampleEntity, Guid>(connection, null, "SampleEntity", "Id");
        var id = Guid.NewGuid();
        repo.Add(new SampleEntity { Id = id });

        Robotico.Result.Result result = repo.Add(new SampleEntity { Id = id });

        Assert.True(result.IsError(out var err));
        Assert.Equal("DUPLICATE", err!.Code);
    }

    [Fact]
    public void GetById_throws_ArgumentNullException_when_id_is_null()
    {
        using var connection = CreateConnection();
        CreateTable(connection);
        var repo = new DapperRepository<SampleEntity, Guid>(connection, null, "SampleEntity", "Id");

        Assert.Throws<ArgumentNullException>(() => repo.GetById(null!));
    }

    [Fact]
    public void Add_throws_ArgumentNullException_when_entity_is_null()
    {
        using var connection = CreateConnection();
        CreateTable(connection);
        var repo = new DapperRepository<SampleEntity, Guid>(connection, null, "SampleEntity", "Id");

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
        using var connection = CreateConnection();
        var uow = new DapperUnitOfWork(connection);
        Robotico.Result.Result result = await uow.CommitAsync();
        Assert.True(result.IsSuccess());
    }
}
