using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Robotico.Result.Errors;

namespace Robotico.Repository.Dapper;

/// <summary>
/// Unit of Work that uses <see cref="DbConnection"/> and <see cref="DbTransaction"/> so that repository operations and commit participate in one transaction.
/// </summary>
/// <remarks>
/// <para>Call <see cref="BeginTransaction"/> before using repositories that use <see cref="Connection"/> and <see cref="Transaction"/>. Call <see cref="IUnitOfWork.CommitAsync"/> to commit.</para>
/// </remarks>
[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Commit maps any provider failure to Result.Error(ExceptionError).")]
public sealed class DapperUnitOfWork : Robotico.Repository.IUnitOfWork, IUnitOfWorkCapabilities
{
    private static readonly UnitOfWorkProfile UnitOfWorkProfileValue = new(
        UnitOfWorkCommitMode.DeferredUntilCommit,
        CommitCoordinatesDomainWrites: true,
        SupportsTransactions: true);

    private DbTransaction? _transaction;

    /// <inheritdoc />
    public UnitOfWorkProfile Capabilities => UnitOfWorkProfileValue;

    /// <summary>
    /// Gets the connection to use for repository operations.
    /// </summary>
    public DbConnection Connection { get; }

    /// <summary>
    /// Creates a unit of work that uses the given connection for repository operations and commit.
    /// </summary>
    /// <param name="connection">The database connection. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> is null.</exception>
    public DapperUnitOfWork(DbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        Connection = connection;
    }

    /// <summary>
    /// Gets the current transaction, or null if <see cref="BeginTransaction"/> has not been called.
    /// </summary>
    public DbTransaction? Transaction => _transaction;

    /// <summary>
    /// Starts a new transaction. Call this before using repositories that share this unit of work.
    /// </summary>
    public void BeginTransaction()
    {
        _transaction = Connection.BeginTransaction();
    }

    /// <inheritdoc />
    public async Task<Robotico.Result.Result> CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_transaction is null)
        {
            return Robotico.Result.Result.Success();
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Robotico.Result.Result.Success();
        }
        catch (Exception ex)
        {
            return Robotico.Result.Result.Error(new ExceptionError(ex));
        }
    }
}
