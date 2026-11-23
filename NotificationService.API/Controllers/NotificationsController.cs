using MediatR;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Common;
using NotificationService.Application.Notifications;

namespace NotificationService.API.Controllers;

public sealed record UpdateNotificationRequest(long ExpectedVersion, int Priority,
    string Kind, string Message, DateTime ScheduledAt);

[Route("api/notifications")]
public sealed class NotificationsController(ISender sender) : ApiController(sender)
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetList(
        [FromQuery] string? userId = null, [FromQuery] string? status = null,
        [FromQuery] string? search = null, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        return Ok(await Sender.Send(new GetNotificationsListQuery(userId, status, search, page, pageSize), ct));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationDto>> GetById(string id, CancellationToken ct)
    {
        var notification = await Sender.Send(new GetNotificationByIdQuery(id), ct);
        Response.Headers.ETag = $"\"{notification.Version}\"";
        return Ok(notification);
    }

    [HttpPost]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<NotificationDto>> Create(CreateNotificationCommand command, CancellationToken ct)
    {
        var notification = await Sender.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = notification.Id }, notification);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(string id, UpdateNotificationRequest request, CancellationToken ct)
    {
        await Sender.Send(new UpdateNotificationCommand(id, request.ExpectedVersion, request.Priority,
            request.Kind, request.Message, request.ScheduledAt), ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string id, [FromQuery] long expectedVersion, CancellationToken ct)
    {
        await Sender.Send(new DeleteNotificationCommand(id, expectedVersion), ct);
        return NoContent();
    }
}
