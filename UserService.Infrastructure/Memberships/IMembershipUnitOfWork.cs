using System.Data;
using UserService.Domain.Memberships;

namespace UserService.Infrastructure.Memberships;

public interface IMembershipRepository : IGenericRepository<Membership>
{
    Task<Membership?> GetWithHistoryAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Membership>> FindByUserAsync(Guid userId, int page, CancellationToken ct);
    Task ChangeStatusAsync(Guid id, string status, long version, CancellationToken ct);
}

public interface ICompanyDirectoryRepository : IGenericRepository<DirectoryCompany>
{
    Task RefreshAsync(DirectoryCompany company, CancellationToken ct);
}

// Deliberately does not inherit the generic repository: this is the ADO.NET example.
public interface IMembershipUserRepository
{
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);
    Task AssignCompanyAsync(Guid userId, Guid companyId, CancellationToken ct);
}

public interface IMembershipUnitOfWork : IAsyncDisposable
{
    IMembershipRepository Memberships { get; }
    ICompanyDirectoryRepository Companies { get; }
    IMembershipUserRepository Users { get; }
    Task BeginAsync(IsolationLevel isolation, CancellationToken ct);
    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}
