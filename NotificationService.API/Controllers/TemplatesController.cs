using MediatR;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Common;
using NotificationService.Application.Templates;

namespace NotificationService.API.Controllers;

public sealed record UpdateTemplateRequest(long ExpectedVersion, string Body);

[Route("api/templates")]
public sealed class TemplatesController(ISender sender) : ApiController(sender)
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TemplateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TemplateDto>>> GetList([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        return Ok(await Sender.Send(new GetTemplatesListQuery(page, pageSize), ct));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TemplateDto>> GetById(string id, CancellationToken ct)
    {
        var template = await Sender.Send(new GetTemplateByIdQuery(id), ct);
        Response.Headers.ETag = $"\"{template.Version}\"";
        return Ok(template);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TemplateDto>> Create(CreateTemplateCommand command, CancellationToken ct)
    {
        var template = await Sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = template.Id }, template);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(string id, UpdateTemplateRequest request, CancellationToken ct)
    {
        await Sender.Send(new UpdateTemplateCommand(id, request.ExpectedVersion, request.Body), ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id, [FromQuery] long expectedVersion, CancellationToken ct)
    {
        await Sender.Send(new DeleteTemplateCommand(id, expectedVersion), ct);
        return NoContent();
    }
}
