using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CompanyService.Domain.Entities;
using CompanyService.Infrastructure.DbContext;

namespace CompanyService.API.Controllers;

[ApiController, Route("api/company-structure")]
public class CompanyStructureController(CompanyDbContext db) : ControllerBase
{

    [HttpGet("tags")]
    public async Task<IActionResult> Tags(CancellationToken ct) => Ok(await db.Tags.ToListAsync(ct));

    [HttpGet("{companyId:guid}/departments")]
    public async Task<IActionResult> Departments(Guid companyId, CancellationToken ct) => Ok(await db.Departments.Where(x => x.CompanyId == companyId).ToListAsync(ct));
    public record DepartmentInput(string Name, decimal Budget);

    [HttpPost("{companyId:guid}/departments")]
    public async Task<IActionResult> AddDepartment(Guid companyId, DepartmentInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 100 || input.Budget <= 0)
            return BadRequest();
        if (!await db.Companies.AnyAsync(x => x.Id == companyId, ct))
            return NotFound();
        if (await db.Departments.AnyAsync(x => x.CompanyId == companyId && x.Name == input.Name, ct))
            return Conflict();
        var item = new Department { Id = Guid.NewGuid(), CompanyId = companyId, Name = input.Name, Budget = input.Budget };
        db.Departments.Add(item);
        await db.SaveChangesAsync(ct);
        return Ok(item);
    }

    [HttpGet("{companyId:guid}/tags")]
    public async Task<IActionResult> CompanyTags(Guid companyId, CancellationToken ct) => Ok(await db.CompanyTags.Where(x => x.CompanyId == companyId).ToListAsync(ct));

    [HttpPut("{companyId:guid}/tags/{tagId:guid}")]
    public async Task<IActionResult> AddTag(Guid companyId, Guid tagId, CancellationToken ct)
    {
        if (!await db.Companies.AnyAsync(x => x.Id == companyId, ct) || !await db.Tags.AnyAsync(x => x.Id == tagId, ct))
            return NotFound();
        if (!await db.CompanyTags.AnyAsync(x => x.CompanyId == companyId && x.TagId == tagId, ct))
        {
            db.CompanyTags.Add(new CompanyTag { CompanyId = companyId, TagId = tagId });
            await db.SaveChangesAsync(ct);
        }
        return NoContent();
    }
}
