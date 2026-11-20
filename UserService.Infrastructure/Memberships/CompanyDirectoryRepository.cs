using Dapper;
using Npgsql;
using UserService.Domain.Memberships;

namespace UserService.Infrastructure.Memberships;

public sealed class CompanyDirectoryRepository(NpgsqlConnection connection, Func<NpgsqlTransaction?> transaction)
    : BaseDapperRepository<DirectoryCompany>(connection, transaction), ICompanyDirectoryRepository
{
    protected override string SelectSql =>
        "SELECT id AS Id, name AS Name, source_version AS SourceVersion, version AS Version FROM company_directory";
    protected override string InsertSql =>
        "INSERT INTO company_directory(id, name, source_version) VALUES (@Id, @Name, @SourceVersion)";
    protected override string UpdateSql =>
        "UPDATE company_directory SET name = @Name, source_version = @SourceVersion WHERE id = @Id AND version = @Version";
    protected override string DeleteSql =>
        "UPDATE company_directory SET is_deleted = true WHERE id = @Id AND version = @Version";

    public async Task RefreshAsync(DirectoryCompany company, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO company_directory(id, name, source_version)
            VALUES (@Id, @Name, @SourceVersion)
            ON CONFLICT(id) DO UPDATE
            SET name = excluded.name, source_version = excluded.source_version
            WHERE company_directory.source_version < excluded.source_version
            """;

        await ExecuteAsync(() => Connection.ExecuteAsync(Command(sql, company, ct)), ct);
    }
}
