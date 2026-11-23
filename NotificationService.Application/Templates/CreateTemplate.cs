using AutoMapper;
using FluentValidation;
using MediatR;
using NotificationService.Application.Common;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Templates;

public sealed record CreateTemplateCommand(string Code, string Channel, string Body) : ICommand<TemplateDto>;

public sealed class CreateTemplateHandler(ITemplateRepository templates, IMapper mapper)
    : IRequestHandler<CreateTemplateCommand, TemplateDto>
{
    public async Task<TemplateDto> Handle(CreateTemplateCommand request, CancellationToken ct)
    {
        var template = new NotificationTemplate(request.Code, request.Channel, request.Body);
        await templates.AddAsync(template, ct);
        return mapper.Map<TemplateDto>(template);
    }
}

public sealed class CreateTemplateValidator : AbstractValidator<CreateTemplateCommand>
{
    public CreateTemplateValidator(ITemplateRepository templates)
    {
        RuleFor(request => request.Code).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Channel).Must(channel => channel is "email" or "inApp");
        RuleFor(request => request.Body).NotEmpty().MaximumLength(4000);
        RuleFor(request => request).MustAsync(async (request, ct) =>
            !await templates.CodeExistsAsync(request.Code.Trim().ToLowerInvariant(), request.Channel, ct))
            .When(request => !string.IsNullOrWhiteSpace(request.Code)
                && request.Channel is "email" or "inApp")
            .WithMessage("A template with this code and channel already exists.");
    }
}
