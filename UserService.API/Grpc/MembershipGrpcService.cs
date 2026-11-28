using Grpc.Core;
    using Npgsql;
    using Platform.Grpc.Memberships.V1;
    using UserService.Application.Memberships;
    using UserService.Domain.Memberships;
using Membership = Platform.Grpc.Memberships.V1.Membership;

    namespace UserService.API.Grpc;

    public sealed class MembershipGrpcService(IMembershipService memberships) : MembershipReader.MembershipReaderBase
    {
        public override async Task<ListMembershipsReply> ListMemberships(ListMembershipsRequest request,
            ServerCallContext context)
        {
            if (!Guid.TryParse(request.UserId, out var userId) || userId == Guid.Empty)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "A valid user ID is required."));

            try
            {
                var items = await memberships.ListAsync(userId, request.Page, context.CancellationToken);
                var reply = new ListMembershipsReply();
                reply.Items.AddRange(items.Select(item => new Membership
                {
                    Id = item.Id.ToString(),
                    UserId = item.UserId.ToString(),
                    CompanyId = item.CompanyId.ToString(),
                    CompanyNameSnapshot = item.CompanyNameSnapshot,
                    RequestedRole = item.RequestedRole,
                    Status = item.Status,
                    Version = item.Version
                }));
                return reply;
            }
            catch (InputValidationException error)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, error.Message));
            }
            catch (ResourceNotFoundException error)
            {
                throw new RpcException(new Status(StatusCode.NotFound, error.Message));
            }
            catch (NpgsqlException error) when (error.IsTransient)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "Membership storage is unavailable."));
            }
        }
    }
