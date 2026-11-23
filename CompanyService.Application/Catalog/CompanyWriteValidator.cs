using FluentValidation;

namespace CompanyService.Application.Catalog;

public sealed class CompanyWriteValidator : AbstractValidator<CompanyWriteRequest>
{
    public CompanyWriteValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Version).GreaterThanOrEqualTo(0);
    }
}
