using CompanyService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanyService.Infrastructure.DbContext;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        CompanyDbContext context, bool seedDemoData, CancellationToken ct = default)
    {
        await context.Database.MigrateAsync(ct);
        if (!seedDemoData)
        {
            return;
        }

        var contosoId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var fabrikamId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var itTagId = Guid.Parse("50000000-0000-0000-0000-000000000001");
        var remoteTagId = Guid.Parse("50000000-0000-0000-0000-000000000002");

        var companies = new[]
        {
            new Company("Contoso", "hello@contoso.example", "Київ, вул. Хрещатик, 1", contosoId),
            new Company("Fabrikam", "hello@fabrikam.example", "Львів, площа Ринок, 1", fabrikamId)
        };
        foreach (var company in companies)
        {
            if (!await context.Companies.IgnoreQueryFilters().AnyAsync(x => x.Id == company.Id, ct))
            {
                context.Companies.Add(company);
            }
        }

        var departments = new[]
        {
            new Department(contosoId, "Engineering", 50000m,
                Guid.Parse("40000000-0000-0000-0000-000000000001")),
            new Department(contosoId, "Quality Assurance", 10000m,
                Guid.Parse("40000000-0000-0000-0000-000000000002")),
            new Department(fabrikamId, "Engineering", 20000m,
                Guid.Parse("40000000-0000-0000-0000-000000000003"))
        };
        foreach (var department in departments)
        {
            if (!await context.Departments.AnyAsync(x => x.Id == department.Id, ct))
            {
                context.Departments.Add(department);
            }
        }

        var links = new[]
        {
            new CompanyTag(contosoId, itTagId),
            new CompanyTag(contosoId, remoteTagId),
            new CompanyTag(fabrikamId, itTagId)
        };
        foreach (var link in links)
        {
            if (!await context.CompanyTags.AnyAsync(
                    x => x.CompanyId == link.CompanyId && x.TagId == link.TagId, ct))
            {
                context.CompanyTags.Add(link);
            }
        }

        await context.SaveChangesAsync(ct);
    }
}
