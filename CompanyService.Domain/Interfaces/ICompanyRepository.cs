using CompanyService.Domain.Entities;
using CompanyService.Domain.Specifications;

namespace CompanyService.Domain.Interfaces;

public interface ICompanyRepository : IGenericRepository<Company>
{
    Task<Company?> GetByEmailAsync(string email, CancellationToken ct);
    Task<Company?> GetDetailsAsync(Guid id, CancellationToken ct);
    Task<(List<Company> Items, int TotalCount)> SearchAsync(Specification<Company> specification, CancellationToken ct);
}
