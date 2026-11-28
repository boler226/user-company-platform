using Platform.Aggregator.Models;
using Platform.Grpc.Memberships.V1;

namespace Platform.Aggregator.Clients;

public sealed class UsersClient(MembershipReader.MembershipReaderClient client, ILogger<UsersClient> logger)
    : ServiceClient(logger, "users-api")
{
    public Task<ServiceResult<IReadOnlyList<MembershipDto>>> GetMembershipsAsync(Guid userId, int page,
        CancellationToken ct) => CallAsync<IReadOnlyList<MembershipDto>>(async (deadline, token) =>
    {
        var reply = await client.ListMembershipsAsync(
            new ListMembershipsRequest { UserId = userId.ToString(), Page = page },
            deadline: deadline, cancellationToken: token);
        return reply.Items.Select(item => new MembershipDto(Guid.Parse(item.Id), Guid.Parse(item.UserId),
            Guid.Parse(item.CompanyId), item.CompanyNameSnapshot, item.RequestedRole, item.Status, item.Version)).ToArray();
    }, ct);
}
