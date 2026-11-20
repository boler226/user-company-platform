using CompanyService.Domain.Entities;

namespace CompanyService.Domain.Specifications;

public sealed class CompanySearchSpecification : Specification<Company>
{
    public CompanySearchSpecification(string? search, Guid? tagId, string sortBy, bool descending, int page, int pageSize)
    {
        Criteria = company =>
            (search == null || company.Name.Contains(search)) &&
            (tagId == null || company.CompanyTags.Any(link => link.TagId == tagId));
        Includes.Add(company => company.Details);
        OrderBy = (sortBy, descending) switch
        {
            ("createdAt", true) => query => query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id),
            ("createdAt", false) => query => query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id),
            ("name", true) => query => query.OrderByDescending(x => x.Name).ThenBy(x => x.Id),
            _ => query => query.OrderBy(x => x.Name).ThenBy(x => x.Id)
        };
        Skip = (page - 1) * pageSize;
        Take = pageSize;
    }
}
