using Microsoft.Data.Sqlite;
using Robotico.Result.Errors;
using Xunit;

namespace Robotico.Repository.Dapper.Tests;

/// <summary>Additional coverage for repository CRUD, unit of work, metadata, cache, and duplicate-key inspector.</summary>
public sealed class DapperAdapterCoverageTests
{
    private static void CreateNamedEntityTable(SqliteConnection connection)
    {
        using SqliteCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE SampleEntityWithName (Id TEXT PRIMARY KEY, Name TEXT NOT NULL DEFAULT '')";
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public void Update_success_changes_row()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        CreateNamedEntityTable(connection);
        DapperRepository<SampleEntityWithName, Guid> repo = new(connection, null, "SampleEntityWithName", "Id");
        Guid id = Guid.NewGuid();
        Assert.True(repo.Add(new SampleEntityWithName { Id = id, Name = "a" }).IsSuccess());

        Robotico.Result.Result updated = repo.Update(new SampleEntityWithName { Id = id, Name = "b" });

        Assert.True(updated.IsSuccess());
        Assert.True(repo.GetById(id).IsSuccess(out SampleEntityWithName? e));
        Assert.Equal("b", e!.Name);
    }

    [Fact]
    public void Update_returns_NOT_FOUND_when_row_missing()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        CreateNamedEntityTable(connection);
        DapperRepository<SampleEntityWithName, Guid> repo = new(connection, null, "SampleEntityWithName", "Id");

        Robotico.Result.Result result = repo.Update(new SampleEntityWithName { Id = Guid.NewGuid(), Name = "x" });

        Assert.True(result.IsError(out IError? err));
        Assert.Equal("NOT_FOUND", err!.Code);
    }

    [Fact]
    public void Update_throws_ArgumentNullException_when_entity_is_null()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        CreateNamedEntityTable(connection);
        DapperRepository<SampleEntityWithName, Guid> repo = new(connection, null, "SampleEntityWithName", "Id");

        Assert.Throws<ArgumentNullException>(() => repo.Update(null!));
    }

    [Fact]
    public void Add_returns_ExceptionError_when_table_does_not_exist()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        DapperRepository<SampleEntity, Guid> repo = new(connection, null, "MissingTable", "Id");

        Robotico.Result.Result result = repo.Add(new SampleEntity { Id = Guid.NewGuid() });

        Assert.True(result.IsError(out IError? err));
        Assert.IsType<ExceptionError>(err);
    }

    [Fact]
    public void Remove_success_deletes_row()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        DapperRepositoryTestsSqlite.CreateSampleEntityTable(connection);
        DapperRepository<SampleEntity, Guid> repo = new(connection, null, "SampleEntity", "Id");
        SampleEntity entity = new() { Id = Guid.NewGuid() };
        Assert.True(repo.Add(entity).IsSuccess());

        Robotico.Result.Result removed = repo.Remove(entity);

        Assert.True(removed.IsSuccess());
        Assert.True(repo.GetById(entity.Id).IsError(out _));
    }

    [Fact]
    public void Remove_returns_NOT_FOUND_when_row_missing()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        DapperRepositoryTestsSqlite.CreateSampleEntityTable(connection);
        DapperRepository<SampleEntity, Guid> repo = new(connection, null, "SampleEntity", "Id");

        Robotico.Result.Result result = repo.Remove(new SampleEntity { Id = Guid.NewGuid() });

        Assert.True(result.IsError(out IError? err));
        Assert.Equal("NOT_FOUND", err!.Code);
    }

    [Fact]
    public void Remove_throws_ArgumentNullException_when_entity_is_null()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        DapperRepositoryTestsSqlite.CreateSampleEntityTable(connection);
        DapperRepository<SampleEntity, Guid> repo = new(connection, null, "SampleEntity", "Id");

        Assert.Throws<ArgumentNullException>(() => repo.Remove(null!));
    }

    [Fact]
    public void Remove_returns_ExceptionError_when_connection_invalid()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        DapperRepositoryTestsSqlite.CreateSampleEntityTable(connection);
        DapperRepository<SampleEntity, Guid> repo = new(connection, null, "SampleEntity", "Id");
        SampleEntity entity = new() { Id = Guid.NewGuid() };
        Assert.True(repo.Add(entity).IsSuccess());
        connection.Close();

        Robotico.Result.Result result = repo.Remove(entity);

        Assert.True(result.IsError(out IError? err));
        Assert.IsType<ExceptionError>(err);
    }

    [Fact]
    public void GetById_returns_ExceptionError_when_connection_invalid()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        DapperRepositoryTestsSqlite.CreateSampleEntityTable(connection);
        DapperRepository<SampleEntity, Guid> repo = new(connection, null, "SampleEntity", "Id");
        connection.Close();

        Robotico.Result.Result<SampleEntity> result = repo.GetById(Guid.NewGuid());

        Assert.True(result.IsError(out IError? err));
        Assert.IsType<ExceptionError>(err);
    }

    [Fact]
    public async Task CommitAsync_with_open_transaction_commits()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        DapperRepositoryTestsSqlite.CreateSampleEntityTable(connection);
        DapperUnitOfWork uow = new(connection);
        uow.BeginTransaction();
        DapperRepository<SampleEntity, Guid> repo = new(connection, uow.Transaction, "SampleEntity", "Id");
        Assert.True(repo.Add(new SampleEntity { Id = Guid.NewGuid() }).IsSuccess());

        Robotico.Result.Result commit = await uow.CommitAsync();

        Assert.True(commit.IsSuccess());
    }

    [Fact]
    public async Task CommitAsync_throws_when_canceled_with_active_transaction()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        DapperUnitOfWork uow = new(connection);
        uow.BeginTransaction();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => uow.CommitAsync(cts.Token));
    }

    [Fact]
    public async Task CommitAsync_returns_ExceptionError_when_commit_fails_after_rollback()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        DapperUnitOfWork uow = new(connection);
        uow.BeginTransaction();
        uow.Transaction!.Rollback();

        Robotico.Result.Result result = await uow.CommitAsync();

        Assert.True(result.IsError(out IError? err));
        Assert.IsType<ExceptionError>(err);
    }

    [Fact]
    public void DapperEntityPersistenceMetadata_BuildInsert_and_Update_sql()
    {
        DapperEntityPersistenceMetadata meta = new(typeof(SampleEntityWithName), "Id");
        string insert = meta.BuildInsertSql("T");
        Assert.Contains("INSERT INTO [T]", insert, StringComparison.Ordinal);
        Assert.Contains("[Name]", insert, StringComparison.Ordinal);
        string update = meta.BuildUpdateSql("T", "Id");
        Assert.Contains("UPDATE [T] SET", update, StringComparison.Ordinal);
        Assert.Contains("[Name] = @Name", update, StringComparison.Ordinal);
    }

    [Fact]
    public void DapperEntityPersistenceMetadata_id_only_entity_uses_fallback_update_set()
    {
        DapperEntityPersistenceMetadata meta = new(typeof(SampleEntity), "Id");
        string update = meta.BuildUpdateSql("E", "Id");
        Assert.Contains("SET [Id] = @Id", update, StringComparison.Ordinal);
    }

    [Fact]
    public void DapperEntityPersistenceMetadata_throws_on_null_args()
    {
        Assert.Throws<ArgumentNullException>(() => new DapperEntityPersistenceMetadata(null!, "Id"));
        Assert.Throws<ArgumentNullException>(() => new DapperEntityPersistenceMetadata(typeof(SampleEntity), null!));
        DapperEntityPersistenceMetadata meta = new(typeof(SampleEntity), "Id");
        Assert.Throws<ArgumentNullException>(() => meta.BuildInsertSql(null!));
        Assert.Throws<ArgumentNullException>(() => meta.BuildUpdateSql(null!, "Id"));
        Assert.Throws<ArgumentNullException>(() => meta.BuildUpdateSql("E", null!));
    }

    [Fact]
    public void DapperEntityPersistenceMetadataCache_Get_throws_when_id_column_null()
    {
        Assert.Throws<ArgumentNullException>(() => DapperEntityPersistenceMetadataCache.Get<SampleEntity>(null!));
    }

    [Fact]
    public void DapperEntityPersistenceMetadataCache_Get_reuses_cached_entry()
    {
        DapperEntityPersistenceMetadata a = DapperEntityPersistenceMetadataCache.Get<SampleEntity>("Id");
        DapperEntityPersistenceMetadata b = DapperEntityPersistenceMetadataCache.Get<SampleEntity>("Id");
        Assert.Same(a, b);
    }

    [Fact]
    public void DapperDuplicateKeyExceptionInspector_throws_on_null()
    {
        Assert.Throws<ArgumentNullException>(() => DapperDuplicateKeyExceptionInspector.IsDuplicateKey(null!));
    }

    [Theory]
    [InlineData("duplicate row")]
    [InlineData("UNIQUE constraint")]
    [InlineData("PRIMARY KEY violation")]
    [InlineData("unique constraint failed")]
    public void IsDuplicateKey_true_from_message(string message)
    {
        Assert.True(DapperDuplicateKeyExceptionInspector.IsDuplicateKey(new InvalidOperationException(message)));
    }

    [Fact]
    public void IsDuplicateKey_true_from_inner_exception_message()
    {
        Exception inner = new("duplicate key");
        Assert.True(DapperDuplicateKeyExceptionInspector.IsDuplicateKey(new Exception("outer", inner)));
    }

    [Fact]
    public void IsDuplicateKey_true_when_sqlite_unique_is_inner_exception()
    {
        SqliteException inner = new("SQLite Error 19: 'UNIQUE constraint failed'.", 19);
        Assert.True(DapperDuplicateKeyExceptionInspector.IsDuplicateKey(new InvalidOperationException("batch", inner)));
    }

    [Fact]
    public void IsDuplicateKey_false_for_unrelated_exception()
    {
        Assert.False(DapperDuplicateKeyExceptionInspector.IsDuplicateKey(new Exception("timeout expired")));
    }

    [Fact]
    public void Repository_uses_custom_table_name_from_ctor()
    {
        using SqliteConnection connection = DapperRepositoryTestsSqlite.CreateConnection();
        using (SqliteCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE CustomTbl (Id TEXT PRIMARY KEY)";
            cmd.ExecuteNonQuery();
        }

        DapperRepository<SampleEntity, Guid> repo = new(connection, null, "CustomTbl", "Id");
        Guid id = Guid.NewGuid();
        Assert.True(repo.Add(new SampleEntity { Id = id }).IsSuccess());
        Assert.True(repo.GetById(id).IsSuccess(out _));
    }
}
