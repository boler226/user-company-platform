using Npgsql;
using NpgsqlTypes;
using UserService.Domain.Memberships;

namespace UserService.Infrastructure.Memberships;

public sealed class AdoMembershipUserRepository(NpgsqlConnection connection, Func<NpgsqlTransaction?> transaction)
    : RepositoryBase(connection, transaction), IMembershipUserRepository
{
    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => ExecuteAsync(async () =>
    {
        const string sql = "SELECT id FROM users WHERE id = @Id AND NOT is_deleted";
        await using var command = new NpgsqlCommand(sql, Connection, Transaction) { CommandTimeout = CommandTimeout };
        command.Parameters.Add("Id", NpgsqlDbType.Uuid).Value = id;
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct);
    }, ct);

    public async Task AssignCompanyAsync(Guid userId, Guid companyId, CancellationToken ct)
    {
        const string sql = """
            UPDATE users SET company_id = @CompanyId
            WHERE id = @UserId AND NOT is_deleted
              AND (company_id IS NULL OR company_id = @CompanyId)
            """;

        var affected = await ExecuteAsync(async () =>
        {
            await using var command = new NpgsqlCommand(sql, Connection, Transaction) { CommandTimeout = CommandTimeout };
            command.Parameters.Add("UserId", NpgsqlDbType.Uuid).Value = userId;
            command.Parameters.Add("CompanyId", NpgsqlDbType.Uuid).Value = companyId;
            return await command.ExecuteNonQueryAsync(ct);
        }, ct);

        if (affected != 1)
        {
            throw new BusinessConflictException("The user is unavailable or already belongs to another company.");
        }
    }
}
