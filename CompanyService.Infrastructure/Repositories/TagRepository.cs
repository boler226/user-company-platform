using CompanyService.Domain.Entities;
using CompanyService.Domain.Interfaces;
using CompanyService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace CompanyService.Infrastructure.Repositories;

public sealed class TagRepository(CompanyDbContext context) : GenericRepository<Tag>(context), ITagRepository
{
    public Task<List<TaggedCompany>> GetCompanyTagsAsync(Guid companyId, CancellationToken ct) =>
        Context.CompanyTags.Where(link => link.CompanyId == companyId)
            .OrderBy(link => link.AddedAt).ThenBy(link => link.TagId)
            .Select(link => new TaggedCompany(link.CompanyId, link.Company.Name, link.TagId, link.Tag.Name, link.AddedAt))
            .ToListAsync(ct);

    public Task<bool> LinkExistsAsync(Guid companyId, Guid tagId, CancellationToken ct) =>
        Context.CompanyTags.AnyAsync(x => x.CompanyId == companyId && x.TagId == tagId, ct);

    public async Task AddLinkAsync(CompanyTag link, CancellationToken ct) => await Context.CompanyTags.AddAsync(link, ct);
}
