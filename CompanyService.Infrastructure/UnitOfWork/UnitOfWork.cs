using CompanyService.Domain.Exceptions;
using CompanyService.Domain.Interfaces;
using CompanyService.Infrastructure.DbContext;
using CompanyService.Infrastructure.UnitOfWork.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CompanyService.Infrastructure.UnitOfWork;

public sealed class UnitOfWork(
    CompanyDbContext context,
    ICompanyRepository companies,
    IDepartmentRepository departments,
    ITagRepository tags) : IUnitOfWork
{
    public ICompanyRepository Companies => companies;
    public IDepartmentRepository Departments => departments;
    public ITagRepository Tags => tags;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The record changed. Reload it and try again.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: "23505" or "23503" or "23514" })
        {
            throw new ConflictException("The operation conflicts with existing data or database constraints.");
        }
    }
}
