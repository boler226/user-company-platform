using CompanyService.Application.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace CompanyService.API.Controllers;

[ApiController]
[Route("api/company-structure")]
[ProducesResponseType<ValidationProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(404)]
[ProducesResponseType<ProblemDetails>(409)]
public class CompanyStructureController(ICompanyService companies) : ControllerBase
{
    [HttpGet("tags")]
    public async Task<ActionResult<List<TagDto>>> Tags(CancellationToken ct) => Ok(await companies.TagsAsync(ct));

    [HttpGet("{companyId:guid}/departments")]
    public async Task<ActionResult<List<DepartmentDto>>> Departments(Guid companyId, CancellationToken ct) =>
        Ok(await companies.DepartmentsAsync(companyId, ct));

    [HttpPost("{companyId:guid}/departments")]
    [ProducesResponseType<DepartmentDto>(201)]
    public async Task<ActionResult<DepartmentDto>> AddDepartment(Guid companyId, DepartmentRequest input, CancellationToken ct)
    {
        var department = await companies.AddDepartmentAsync(companyId, input, ct);
        return CreatedAtAction(nameof(Department), new { companyId, departmentId = department.Id }, department);
    }

    [HttpGet("{companyId:guid}/departments/{departmentId:guid}")]
    public async Task<ActionResult<DepartmentDto>> Department(Guid companyId, Guid departmentId, CancellationToken ct) =>
        Ok(await companies.GetDepartmentAsync(companyId, departmentId, ct));

    [HttpGet("{companyId:guid}/tags")]
    public async Task<ActionResult<List<CompanyTagDto>>> CompanyTags(Guid companyId, CancellationToken ct) =>
        Ok(await companies.CompanyTagsAsync(companyId, ct));

    [HttpPut("{companyId:guid}/tags/{tagId:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> AddTag(Guid companyId, Guid tagId, CancellationToken ct)
    {
        await companies.AddTagAsync(companyId, tagId, ct);
        return NoContent();
    }
}
