using FluentValidation;

namespace CompanyService.Application.Catalog;

public sealed class CompanyQueryValidator : AbstractValidator<CompanyQuery>
{
    public CompanyQueryValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 100000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.SortBy).Must(value => value is "name" or "createdAt");
        RuleFor(x => x.SortDir).Must(value => value is "asc" or "desc");
    }
}
