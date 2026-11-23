using AutoMapper;
using FluentValidation;
using MediatR;
using MongoDB.Bson;
using NotificationService.Application.Common;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.ValueObjects;

namespace NotificationService.Application.Notifications;

public sealed record CreateNotificationCommand(string TemplateId, string UserId, string Email,
    string DisplayName, string? CompanyId, string? CompanyName, int Priority,
    string Kind, string Message, DateTime ScheduledAt) : ICommand<NotificationDto>;

public sealed class CreateNotificationHandler(INotificationRepository notifications, IMapper mapper)
    : IRequestHandler<CreateNotificationCommand, NotificationDto>
{
    public async Task<NotificationDto> Handle(CreateNotificationCommand request, CancellationToken ct)
    {
        var recipient = new Recipient(request.UserId, request.Email, request.DisplayName);
        var company = request.CompanyId is null
            ? null
            : new CompanySnapshot(request.CompanyId, request.CompanyName!);
        var notification = new Notification(ObjectId.Parse(request.TemplateId), recipient, company,
            request.Priority, request.Kind, request.Message, request.ScheduledAt);

        await notifications.AddAsync(notification, ct);
        return mapper.Map<NotificationDto>(notification);
    }
}

public sealed class CreateNotificationValidator : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationValidator(ITemplateRepository templates)
    {
        RuleFor(request => request.TemplateId).Cascade(CascadeMode.Stop)
            .Must(ValidationRules.IsObjectId).WithMessage("Template ID must be a non-empty ObjectId.")
            .MustAsync((id, ct) => templates.ExistsAsync(ObjectId.Parse(id), ct))
            .WithMessage("The selected template does not exist.");
        RuleFor(request => request.UserId).Must(ValidationRules.IsUuid)
            .WithMessage("User ID must be a non-empty UUID.");
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(request => request.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(request => request.CompanyId).Must(ValidationRules.IsUuid)
            .When(request => request.CompanyId is not null);
        RuleFor(request => request.CompanyName).NotEmpty().MaximumLength(200)
            .When(request => request.CompanyId is not null);
        RuleFor(request => request.CompanyName).Null().When(request => request.CompanyId is null);
        RuleFor(request => request.Priority).InclusiveBetween(1, 5);
        RuleFor(request => request.Kind).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Message).NotEmpty().MaximumLength(4000);
        RuleFor(request => request.ScheduledAt).Must(value => value.Kind == DateTimeKind.Utc)
            .WithMessage("ScheduledAt must be a UTC timestamp ending in Z.");
    }
}
