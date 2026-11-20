using CompanyService.Domain.Interfaces;
using CompanyService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace CompanyService.Infrastructure.Repositories;

public class GenericRepository<T>(CompanyDbContext context) : IGenericRepository<T> where T : class
{
    protected CompanyDbContext Context => context;
    protected DbSet<T> Set => context.Set<T>();

    public async Task AddAsync(T entity, CancellationToken ct) => await Set.AddAsync(entity, ct);
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct) => await Set.FindAsync([id], ct);
    public virtual Task<List<T>> GetAllAsync(CancellationToken ct) => Set.AsNoTracking().ToListAsync(ct);
    public void Update(T entity) => Set.Update(entity);
    public void Delete(T entity) => Set.Remove(entity);
}
