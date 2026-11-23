using FluentValidation;
using MediatR;
using NotificationService.Application.Common;
using NotificationService.Domain.Exceptions;

namespace NotificationService.Application.Notifications;

public sealed record GetNotificationByIdQuery(string Id) : IQuery<NotificationDto>;

public sealed class GetNotificationByIdHandler(INotificationQueries queries)
    : IRequestHandler<GetNotificationByIdQuery, NotificationDto>
{
    public async Task<NotificationDto> Handle(GetNotificationByIdQuery request, CancellationToken ct)
    {
        return await queries.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Notification was not found.");
    }
}

public sealed class GetNotificationByIdValidator : AbstractValidator<GetNotificationByIdQuery>
{
    public GetNotificationByIdValidator()
    {
        RuleFor(request => request.Id).Must(ValidationRules.IsObjectId);
    }
}
