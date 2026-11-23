using FluentValidation;
using MediatR;
using MongoDB.Bson;
using NotificationService.Application.Common;
using NotificationService.Domain.Exceptions;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Notifications;

public sealed record DeleteNotificationCommand(string Id, long ExpectedVersion) : ICommand;

public sealed class DeleteNotificationHandler(INotificationRepository repository)
    : IRequestHandler<DeleteNotificationCommand>
{
    public async Task Handle(DeleteNotificationCommand request, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(ObjectId.Parse(request.Id), ct)
            ?? throw new NotFoundException("Notification was not found.");
        if (entity.Version != request.ExpectedVersion)
            throw new ConflictException("Notification has changed. Reload it before deleting.");

        entity.Delete();
        await repository.UpdateAsync(entity, request.ExpectedVersion, ct);
    }
}

public sealed class DeleteNotificationValidator : AbstractValidator<DeleteNotificationCommand>
{
    public DeleteNotificationValidator()
    {
        RuleFor(request => request.Id).Must(ValidationRules.IsObjectId);
        RuleFor(request => request.ExpectedVersion).InclusiveBetween(1, long.MaxValue - 1);
    }
}
