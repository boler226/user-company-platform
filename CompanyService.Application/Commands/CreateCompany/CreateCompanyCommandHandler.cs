using CompanyService.Domain.Entities;
using CompanyService.Infrastructure.UnitOfWork.Interfaces;
using FluentValidation;
using MediatR;

namespace CompanyService.Application.Commands.CreateCompany;

public class CreateCompanyCommandHandler(
    IUnitOfWork unitOfWork,
    IValidator<CreateCompanyCommand> validator
) : IRequestHandler<CreateCompanyCommand, Guid>
{
    public async Task<Guid> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var company = new Company(request.Name, request.Email, request.Address);

        await unitOfWork.Companies.AddAsync(company, cancellationToken);
        await unitOfWork.SaveChangesAsync();

        return company.Id;
    }
}
