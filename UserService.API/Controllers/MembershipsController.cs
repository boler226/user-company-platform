using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Memberships;

namespace UsersService.API.Controllers;

[ApiController]
[Route("api/memberships")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
public class MembershipsController(IMembershipService memberships) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MembershipDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await memberships.GetAsync(id, ct));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MembershipDto>>> List(
        Guid userId, CancellationToken ct, [Range(1, 100000)] int page = 1) =>
        Ok(await memberships.ListAsync(userId, page, ct));

    [HttpPost]
    [ProducesResponseType<MembershipDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MembershipDto>> Create(CreateMembershipRequest input, CancellationToken ct)
    {
        var created = await memberships.CreateAsync(input, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, EditMembershipRequest input, CancellationToken ct)
    {
        await memberships.UpdateAsync(id, input, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, ChangeMembershipStatusRequest input, CancellationToken ct)
    {
        await memberships.ChangeStatusAsync(id, input, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [Range(1, long.MaxValue)] long version, CancellationToken ct)
    {
        await memberships.DeleteAsync(id, version, ct);
        return NoContent();
    }
}
