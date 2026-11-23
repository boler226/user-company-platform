using FluentValidation;
using MediatR;
using MongoDB.Bson;
using NotificationService.Application.Common;
using NotificationService.Domain.Exceptions;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Templates;

public sealed record UpdateTemplateCommand(string Id, long ExpectedVersion, string Body) : ICommand;

public sealed class UpdateTemplateHandler(ITemplateRepository templates) : IRequestHandler<UpdateTemplateCommand>
{
    public async Task Handle(UpdateTemplateCommand request, CancellationToken ct)
    {
        var template = await templates.GetByIdAsync(ObjectId.Parse(request.Id), ct)
            ?? throw new NotFoundException("Template was not found.");
        if (template.Version != request.ExpectedVersion)
            throw new ConflictException("Template has changed. Reload it before editing.");

        template.Update(request.Body);
        await templates.UpdateAsync(template, request.ExpectedVersion, ct);
    }
}

public sealed class UpdateTemplateValidator : AbstractValidator<UpdateTemplateCommand>
{
    public UpdateTemplateValidator()
    {
        RuleFor(request => request.Id).Must(ValidationRules.IsObjectId);
        RuleFor(request => request.ExpectedVersion).InclusiveBetween(1, long.MaxValue - 1);
        RuleFor(request => request.Body).NotEmpty().MaximumLength(4000);
    }
}
