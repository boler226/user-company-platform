using FluentValidation;
using MediatR;
using NotificationService.Application.Common;

namespace NotificationService.Application.Templates;

public sealed record GetTemplatesListQuery(int Page = 1, int PageSize = 20) : IQuery<PagedResult<TemplateDto>>;

public sealed class GetTemplatesListHandler(ITemplateQueries queries)
    : IRequestHandler<GetTemplatesListQuery, PagedResult<TemplateDto>>
{
    public Task<PagedResult<TemplateDto>> Handle(GetTemplatesListQuery request, CancellationToken ct) =>
        queries.GetListAsync(request.Page, request.PageSize, ct);
}

public sealed class GetTemplatesListValidator : AbstractValidator<GetTemplatesListQuery>
{
    public GetTemplatesListValidator()
    {
        RuleFor(request => request.Page).InclusiveBetween(1, 1000000);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
    }
}
