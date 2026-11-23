namespace CompanyService.Domain.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task AddAsync(T entity, CancellationToken ct);
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<T>> GetAllAsync(CancellationToken ct);
    void Update(T entity);
    void Delete(T entity);
}
