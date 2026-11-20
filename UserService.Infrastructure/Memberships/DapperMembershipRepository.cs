using Dapper;
using Npgsql;
using UserService.Domain.Memberships;

namespace UserService.Infrastructure.Memberships;

public sealed class DapperMembershipRepository(NpgsqlConnection connection, Func<NpgsqlTransaction?> transaction)
    : BaseDapperRepository<Membership>(connection, transaction), IMembershipRepository
{
    private const string Columns = """
        id AS Id, user_id AS UserId, company_id AS CompanyId,
        company_name_snapshot AS CompanyNameSnapshot, requested_role AS RequestedRole,
        status AS Status, version AS Version
        """;

    protected override string SelectSql => $"SELECT {Columns} FROM membership_applications";
    protected override string InsertSql => "CALL create_application(@Id, @UserId, @CompanyId, @RequestedRole)";
    protected override string UpdateSql => "CALL update_application(@Id, @RequestedRole, @Version)";
    protected override string DeleteSql => "CALL delete_application(@Id, @Version)";

    public Task<IReadOnlyList<Membership>> FindByUserAsync(Guid userId, int page, CancellationToken ct) =>
        ExecuteAsync<IReadOnlyList<Membership>>(async () => (await Connection.QueryAsync<Membership>(Command(
            SelectSql + " WHERE user_id = @UserId AND NOT is_deleted ORDER BY created_at DESC, id LIMIT 20 OFFSET @Offset",
            new { UserId = userId, Offset = (page - 1) * 20 }, ct))).AsList(), ct);

    public Task<Membership?> GetWithHistoryAsync(Guid id, CancellationToken ct) => ExecuteAsync<Membership?>(async () =>
    {
        const string sql = """
            SELECT a.id AS Id, a.user_id AS UserId, a.company_id AS CompanyId,
                   a.company_name_snapshot AS CompanyNameSnapshot, a.requested_role AS RequestedRole,
                   a.status AS Status, a.version AS Version,
                   h.id AS Id, h.old_status AS OldStatus, h.new_status AS NewStatus, h.created_at AS CreatedAt
            FROM membership_applications a
            LEFT JOIN application_status_history h ON h.application_id = a.id
            WHERE a.id = @Id AND NOT a.is_deleted
            ORDER BY h.created_at, h.id
            """;

        Membership? result = null;
        await Connection.QueryAsync<Membership, MembershipHistory, Membership>(
            Command(sql, new { Id = id }, ct),
            (membership, history) =>
            {
                result ??= membership;
                if (history is not null)
                {
                    result.History.Add(history);
                }
                return result;
            }, splitOn: "Id");

        return result;
    }, ct);

    public async Task ChangeStatusAsync(Guid id, string status, long version, CancellationToken ct) =>
        await ExecuteAsync(() => Connection.ExecuteAsync(Command(
            "CALL change_application_status(@Id, @Status, @Version)",
            new { Id = id, Status = status, Version = version }, ct)), ct);
}
