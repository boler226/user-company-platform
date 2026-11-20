using System.Data;
using Npgsql;
using UserService.Domain.Memberships;

namespace UserService.Infrastructure.Memberships;

public sealed class MembershipUnitOfWork : IMembershipUnitOfWork
{
    private readonly NpgsqlConnection connection;
    private NpgsqlTransaction? transaction;

    public MembershipUnitOfWork(string connectionString)
    {
        connection = new NpgsqlConnection(connectionString);
        Memberships = new DapperMembershipRepository(connection, () => transaction);
        Companies = new CompanyDirectoryRepository(connection, () => transaction);
        Users = new AdoMembershipUserRepository(connection, () => transaction);
    }

    public IMembershipRepository Memberships { get; }
    public ICompanyDirectoryRepository Companies { get; }
    public IMembershipUserRepository Users { get; }

    public async Task BeginAsync(IsolationLevel isolation, CancellationToken ct)
    {
        if (transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active.");
        }
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }
        transaction = await connection.BeginTransactionAsync(isolation, ct);
    }

    public async Task CommitAsync(CancellationToken ct)
    {
        if (transaction is null)
        {
            throw new InvalidOperationException("No active transaction.");
        }
        try
        {
            await transaction.CommitAsync(ct);
        }
        catch (PostgresException exception) when (exception.SqlState is "40001" or "40P01")
        {
            throw new BusinessConflictException("The transaction conflicted with another request. Reload and retry.");
        }
        await transaction.DisposeAsync();
        transaction = null;
    }

    public async Task RollbackAsync(CancellationToken ct)
    {
        if (transaction is null)
        {
            return;
        }
        try
        {
            await transaction.RollbackAsync(ct);
        }
        finally
        {
            await transaction.DisposeAsync();
            transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await RollbackAsync(CancellationToken.None);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
