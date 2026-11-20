using CompanyService.Domain.Specifications;
using Microsoft.EntityFrameworkCore;

namespace CompanyService.Infrastructure.Repositories;

public static class SpecificationExtensions
{
    public static IQueryable<T> WithSpecification<T>(
        this IQueryable<T> query, Specification<T> specification, bool countOnly = false)
        where T : class
    {
        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        if (countOnly)
        {
            return query;
        }

        foreach (var include in specification.Includes)
        {
            query = query.Include(include);
        }

        if (specification.OrderBy is not null)
        {
            query = specification.OrderBy(query);
        }

        if (specification.Skip.HasValue)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        return query;
    }
}
