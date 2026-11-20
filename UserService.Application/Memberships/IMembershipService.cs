namespace UserService.Application.Memberships;

public interface IMembershipService
{
    Task<MembershipDto> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<MembershipDto>> ListAsync(Guid userId, int page, CancellationToken ct);
    Task<MembershipDto> CreateAsync(CreateMembershipRequest input, CancellationToken ct);
    Task UpdateAsync(Guid id, EditMembershipRequest input, CancellationToken ct);
    Task ChangeStatusAsync(Guid id, ChangeMembershipStatusRequest input, CancellationToken ct);
    Task DeleteAsync(Guid id, long version, CancellationToken ct);
}
