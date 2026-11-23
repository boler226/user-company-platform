using FluentValidation;
using MediatR;
using NotificationService.Application.Common;

namespace NotificationService.Application.Notifications;

public sealed record GetNotificationsListQuery(string? UserId = null, string? Status = null,
    string? Search = null, int Page = 1, int PageSize = 20) : IQuery<PagedResult<NotificationDto>>;

public sealed class GetNotificationsListHandler(INotificationQueries queries)
    : IRequestHandler<GetNotificationsListQuery, PagedResult<NotificationDto>>
{
    public Task<PagedResult<NotificationDto>> Handle(GetNotificationsListQuery request, CancellationToken ct)
    {
        var userId = request.UserId is null ? null : Guid.Parse(request.UserId).ToString();
        return queries.GetListAsync(userId, request.Status, request.Search, request.Page, request.PageSize, ct);
    }
}

public sealed class GetNotificationsListValidator : AbstractValidator<GetNotificationsListQuery>
{
    public GetNotificationsListValidator()
    {
        RuleFor(request => request.UserId).Must(ValidationRules.IsUuid).When(request => request.UserId is not null);
        RuleFor(request => request.Status).Must(value => value is "Pending" or "Sent" or "Failed")
            .When(request => request.Status is not null);
        RuleFor(request => request.Search).NotEmpty().MaximumLength(200).When(request => request.Search is not null);
        RuleFor(request => request.Page).InclusiveBetween(1, 1000000);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
    }
}
