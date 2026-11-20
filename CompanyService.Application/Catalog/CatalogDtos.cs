using System.ComponentModel.DataAnnotations;
using CompanyService.Application.DTOs;

namespace CompanyService.Application.Catalog;

public record CompanyWriteRequest(
    [Required, StringLength(100)] string Name,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(300)] string Address,
    long Version = 0);

public record DepartmentRequest(
    [Required, StringLength(100)] string Name,
    [Range(typeof(decimal), "0.01", "9999999999999999.99")] decimal Budget);

public class CompanyQuery
{
    [Range(1, 100000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [StringLength(100)]
    public string? Search { get; set; }

    public Guid? TagId { get; set; }
    public string SortBy { get; set; } = "name";
    public string SortDir { get; set; } = "asc";
}

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}

public record DepartmentDto(Guid Id, Guid CompanyId, string Name, decimal Budget, long Version);
public record TagDto(Guid Id, string Name);
public record CompanyTagDto(Guid CompanyId, string CompanyName, Guid TagId, string TagName, DateTime AddedAt);

public class CompanyView : CompanyDto
{
    public List<DepartmentDto> Departments { get; set; } = [];
    public List<TagDto> Tags { get; set; } = [];
}
