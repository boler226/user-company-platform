using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using MongoDB.Driver;
using NotificationService.Application.Notifications;
using Platform.Grpc.Notifications.V1;

namespace NotificationService.API.Grpc;

public sealed class NotificationGrpcService(ISender sender) : NotificationReader.NotificationReaderBase
{
    public override async Task<ListNotificationsReply> ListNotifications(ListNotificationsRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId) || userId == Guid.Empty)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "A valid user ID is required."));

        try
        {
            var page = await sender.Send(new GetNotificationsListQuery(userId.ToString(),
                Page: request.Page, PageSize: request.PageSize), context.CancellationToken);
            var reply = new ListNotificationsReply
            {
                TotalCount = page.TotalCount,
                Page = page.Page,
                PageSize = page.PageSize
            };
            foreach (var item in page.Items)
            {
                var notification = new Notification
                {
                    Id = item.Id,
                    Status = item.Status,
                    Priority = item.Priority,
                    CreatedAt = Timestamp.FromDateTime(item.CreatedAt.ToUniversalTime()),
                    Version = item.Version
                };
                notification.Payload.Add(item.Payload);
                reply.Items.Add(notification);
            }

            return reply;
        }
        catch (ValidationException error)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument,
                string.Join("; ", error.Errors.Select(failure => failure.ErrorMessage))));
        }
        catch (Exception error) when (error is MongoConnectionException or TimeoutException)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, "Notification storage is unavailable."));
        }
    }
}
