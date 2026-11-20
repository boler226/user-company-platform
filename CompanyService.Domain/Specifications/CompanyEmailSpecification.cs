using CompanyService.Domain.Entities;

namespace CompanyService.Domain.Specifications;

public sealed class CompanyEmailSpecification : Specification<Company>
{
    public CompanyEmailSpecification(string email)
    {
        Criteria = company => company.Email == email;
        Includes.Add(company => company.Details);
    }
}
