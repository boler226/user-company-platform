using CompanyService.Application.Catalog;
using CompanyService.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CompanyService.API.Controllers;

[ApiController]
[Route("api/companies")]
[ProducesResponseType<ValidationProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(404)]
[ProducesResponseType<ProblemDetails>(409)]
public class CompaniesController(ICompanyService companies) : ControllerBase
{
    [HttpGet]
    [HttpGet("GetAll")]
    public async Task<ActionResult<PagedResult<CompanyDto>>> Search([FromQuery] CompanyQuery query, CancellationToken ct) =>
        Ok(await companies.SearchAsync(query, ct));

    [HttpGet("{id:guid}")]
    [HttpGet("GetById/{id:guid}")]
    public async Task<ActionResult<CompanyView>> Get(Guid id, CancellationToken ct) => Ok(await companies.GetAsync(id, ct));

    [HttpPost]
    [HttpPost("Create")]
    [ProducesResponseType<CompanyView>(201)]
    public async Task<ActionResult<CompanyView>> Create(CompanyWriteRequest input, CancellationToken ct)
    {
        var company = await companies.CreateAsync(input, ct);
        return CreatedAtAction(nameof(Get), new { id = company.Id }, company);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Update(Guid id, CompanyWriteRequest input, CancellationToken ct)
    {
        await companies.UpdateAsync(id, input, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await companies.DeleteAsync(id, ct);
        return NoContent();
    }
}
