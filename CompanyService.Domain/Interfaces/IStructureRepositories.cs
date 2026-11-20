using CompanyService.Domain.Entities;

namespace CompanyService.Domain.Interfaces;

public interface IDepartmentRepository : IGenericRepository<Department>
{
    Task<IReadOnlyCollection<Department>> LoadForCompanyAsync(Company company, CancellationToken ct);
    Task<bool> NameExistsAsync(Guid companyId, string name, CancellationToken ct);
}

public record TaggedCompany(Guid CompanyId, string CompanyName, Guid TagId, string TagName, DateTime AddedAt);

public interface ITagRepository : IGenericRepository<Tag>
{
    Task<List<TaggedCompany>> GetCompanyTagsAsync(Guid companyId, CancellationToken ct);
    Task<bool> LinkExistsAsync(Guid companyId, Guid tagId, CancellationToken ct);
    Task AddLinkAsync(CompanyTag link, CancellationToken ct);
}
