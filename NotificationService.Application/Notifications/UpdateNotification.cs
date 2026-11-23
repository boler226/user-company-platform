using FluentValidation;
using MediatR;
using MongoDB.Bson;
using NotificationService.Application.Common;
using NotificationService.Domain.Exceptions;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Notifications;

public sealed record UpdateNotificationCommand(string Id, long ExpectedVersion, int Priority,
    string Kind, string Message, DateTime ScheduledAt) : ICommand;

public sealed class UpdateNotificationHandler(INotificationRepository notifications)
    : IRequestHandler<UpdateNotificationCommand>
{
    public async Task Handle(UpdateNotificationCommand request, CancellationToken ct)
    {
        var notification = await notifications.GetByIdAsync(ObjectId.Parse(request.Id), ct)
            ?? throw new NotFoundException("Notification was not found.");
        if (notification.Version != request.ExpectedVersion)
            throw new ConflictException("Notification has changed. Reload it before editing.");

        notification.Update(request.Priority, request.Kind, request.Message, request.ScheduledAt);
        await notifications.UpdateAsync(notification, request.ExpectedVersion, ct);
    }
}

public sealed class UpdateNotificationValidator : AbstractValidator<UpdateNotificationCommand>
{
    public UpdateNotificationValidator()
    {
        RuleFor(request => request.Id).Must(ValidationRules.IsObjectId);
        RuleFor(request => request.ExpectedVersion).InclusiveBetween(1, long.MaxValue - 1);
        RuleFor(request => request.Priority).InclusiveBetween(1, 5);
        RuleFor(request => request.Kind).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Message).NotEmpty().MaximumLength(4000);
        RuleFor(request => request.ScheduledAt).Must(value => value.Kind == DateTimeKind.Utc)
            .WithMessage("ScheduledAt must be a UTC timestamp ending in Z.");
    }
}
