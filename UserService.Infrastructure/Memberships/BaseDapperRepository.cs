using Dapper;
using Npgsql;
using UserService.Domain.Memberships;

namespace UserService.Infrastructure.Memberships;

public abstract class BaseDapperRepository<T>(
    NpgsqlConnection connection,
    Func<NpgsqlTransaction?> currentTransaction) : RepositoryBase(connection, currentTransaction), IGenericRepository<T>
    where T : class
{
    // All SQL is defined by concrete repositories, never supplied by an HTTP client.
    protected abstract string SelectSql { get; }
    protected abstract string InsertSql { get; }
    protected abstract string UpdateSql { get; }
    protected abstract string DeleteSql { get; }

    protected CommandDefinition Command(string sql, object? parameters, CancellationToken ct) =>
        new(sql, parameters, Transaction, commandTimeout: CommandTimeout, cancellationToken: ct);

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct) => ExecuteAsync(
        () => Connection.QuerySingleOrDefaultAsync<T>(Command(SelectSql + " WHERE id = @Id AND NOT is_deleted", new { Id = id }, ct)), ct);

    public Task<IReadOnlyList<T>> GetAllAsync(int page, CancellationToken ct) => ExecuteAsync<IReadOnlyList<T>>(
        async () => (await Connection.QueryAsync<T>(Command(
            SelectSql + " WHERE NOT is_deleted ORDER BY id LIMIT 20 OFFSET @Offset",
            new { Offset = (page - 1) * 20 }, ct))).AsList(), ct);

    public async Task AddAsync(T entity, CancellationToken ct) =>
        await ExecuteAsync(() => Connection.ExecuteAsync(Command(InsertSql, entity, ct)), ct);

    public async Task UpdateAsync(T entity, CancellationToken ct) =>
        await WriteAsync(UpdateSql, entity, ct);

    public async Task DeleteAsync(Guid id, long version, CancellationToken ct) =>
        await WriteAsync(DeleteSql, new { Id = id, Version = version }, ct);

    private async Task WriteAsync(string sql, object parameters, CancellationToken ct)
    {
        var affected = await ExecuteAsync(() => Connection.ExecuteAsync(Command(sql, parameters, ct)), ct);
        // CALL returns -1; its procedure raises errors itself. An UPDATE returning 0 is a stale write.
        if (affected == 0)
        {
            throw new BusinessConflictException("The record is missing or its version has changed.");
        }
    }
}
