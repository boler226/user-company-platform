using CompanyService.Domain.Interfaces;

namespace CompanyService.Infrastructure.UnitOfWork.Interfaces;

public interface IUnitOfWork
{
    ICompanyRepository Companies { get; }
    IDepartmentRepository Departments { get; }
    ITagRepository Tags { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
