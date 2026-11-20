using CompanyService.Domain.Entities;
using CompanyService.Domain.Interfaces;
using CompanyService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace CompanyService.Infrastructure.Repositories;

public sealed class DepartmentRepository(CompanyDbContext context) : GenericRepository<Department>(context), IDepartmentRepository
{
    public async Task<IReadOnlyCollection<Department>> LoadForCompanyAsync(Company company, CancellationToken ct)
    {
        await Context.Entry(company).Collection(x => x.Departments).LoadAsync(ct);
        return company.Departments;
    }

    public Task<bool> NameExistsAsync(Guid companyId, string name, CancellationToken ct) =>
        Set.AnyAsync(x => x.CompanyId == companyId && x.Name == name, ct);
}
