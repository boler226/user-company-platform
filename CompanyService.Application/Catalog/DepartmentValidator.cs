using FluentValidation;

namespace CompanyService.Application.Catalog;

public sealed class DepartmentValidator : AbstractValidator<DepartmentRequest>
{
    public DepartmentValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Budget).GreaterThan(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);
    }
}
