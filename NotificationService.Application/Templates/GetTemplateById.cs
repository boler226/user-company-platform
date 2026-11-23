using FluentValidation;
using MediatR;
using NotificationService.Application.Common;
using NotificationService.Domain.Exceptions;

namespace NotificationService.Application.Templates;

public sealed record GetTemplateByIdQuery(string Id) : IQuery<TemplateDto>;

public sealed class GetTemplateByIdHandler(ITemplateQueries queries)
    : IRequestHandler<GetTemplateByIdQuery, TemplateDto>
{
    public async Task<TemplateDto> Handle(GetTemplateByIdQuery request, CancellationToken ct)
    {
        return await queries.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException("Template was not found.");
    }
}

public sealed class GetTemplateByIdValidator : AbstractValidator<GetTemplateByIdQuery>
{
    public GetTemplateByIdValidator()
    {
        RuleFor(request => request.Id).Must(ValidationRules.IsObjectId);
    }
}
