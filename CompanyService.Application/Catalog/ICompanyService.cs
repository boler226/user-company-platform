using CompanyService.Application.DTOs;

namespace CompanyService.Application.Catalog;

public interface ICompanyService
{
    Task<DepartmentDto> GetDepartmentAsync(Guid companyId, Guid departmentId, CancellationToken ct);
    Task<PagedResult<CompanyDto>> SearchAsync(CompanyQuery query, CancellationToken ct);
    Task<CompanyView> GetAsync(Guid id, CancellationToken ct);
    Task<CompanyView> CreateAsync(CompanyWriteRequest input, CancellationToken ct);
    Task UpdateAsync(Guid id, CompanyWriteRequest input, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<List<DepartmentDto>> DepartmentsAsync(Guid id, CancellationToken ct);
    Task<DepartmentDto> AddDepartmentAsync(Guid id, DepartmentRequest input, CancellationToken ct);
    Task<List<TagDto>> TagsAsync(CancellationToken ct);
    Task<List<CompanyTagDto>> CompanyTagsAsync(Guid id, CancellationToken ct);
    Task AddTagAsync(Guid companyId, Guid tagId, CancellationToken ct);
}
