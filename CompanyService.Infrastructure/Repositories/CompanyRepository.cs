using CompanyService.Domain.Entities;
using CompanyService.Domain.Interfaces;
using CompanyService.Domain.Specifications;
using CompanyService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace CompanyService.Infrastructure.Repositories;

public sealed class CompanyRepository(CompanyDbContext context) : GenericRepository<Company>(context), ICompanyRepository
{
    public override Task<Company?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Set.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id, ct);

    public override Task<List<Company>> GetAllAsync(CancellationToken ct) =>
        Set.AsNoTracking().Include(x => x.Details).ToListAsync(ct);

    public Task<Company?> GetByEmailAsync(string email, CancellationToken ct) =>
        Set.IgnoreQueryFilters()
            .WithSpecification(new CompanyEmailSpecification(email))
            .FirstOrDefaultAsync(ct);

    public Task<Company?> GetDetailsAsync(Guid id, CancellationToken ct) =>
        Set.Include(x => x.Details)
            .Include(x => x.Departments)
            .Include(x => x.CompanyTags).ThenInclude(x => x.Tag)
            .AsSingleQuery()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<(List<Company> Items, int TotalCount)> SearchAsync(Specification<Company> specification, CancellationToken ct)
    {
        var query = Set.AsNoTracking();
        var total = await query.WithSpecification(specification, countOnly: true).CountAsync(ct);
        var items = await query.WithSpecification(specification).ToListAsync(ct);
        return (items, total);
    }
}
