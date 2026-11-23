using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Models;
using Platform.Aggregator.Services;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/aggregator/users")]
public sealed class OverviewController(UserOverviewService overview) : ControllerBase
{
    [HttpGet("{userId:guid}/overview")]
    public async Task<ActionResult<UserOverviewDto>> Get(Guid userId,
        [FromQuery, Range(1, 100000)] int page = 1, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
            return BadRequest(new ProblemDetails { Status = 400, Title = "A non-empty user ID is required." });

        return Ok(await overview.GetAsync(userId, page, ct));
    }
}
