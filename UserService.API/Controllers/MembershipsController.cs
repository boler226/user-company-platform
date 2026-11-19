using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using UserService.Infrastructure.Repositories;

namespace UsersService.API.Controllers;

[ApiController, Route("api/memberships")]
public class MembershipsController(MembershipRepository repository, IHttpClientFactory clients) : ControllerBase
{
    public record CreateRequest(
        Guid UserId,
        Guid CompanyId,
        [Required, StringLength(50, MinimumLength = 1)] string Role);

    public record EditRequest(
        [Required, StringLength(50, MinimumLength = 1)] string Role,
        long Version);

    public record StatusRequest([Required] string Status, long Version);

    private async Task<IActionResult> Execute(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (PostgresException e) when (e.SqlState == "P0002")
        {
            return NotFound(new { error = e.MessageText });
        }
        catch (PostgresException e) when (e.SqlState is "40001" or "23505")
        {
            return Conflict(new { error = e.MessageText });
        }
        catch (PostgresException e) when (e.SqlState is "P0001" or "23503" or "23514")
        {
            return BadRequest(new { error = e.MessageText });
        }
    }

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        Execute(async () => Ok(await repository.ExecuteAsync(
            "CALL get_application(@p0, NULL::jsonb)", ct, id)));

    [HttpGet]
    public Task<IActionResult> List(Guid userId, CancellationToken ct, int page = 1) => Execute(async () =>
    {
        if (page < 1 || page > 100000)
        {
            return BadRequest();
        }
        return Ok(await repository.ExecuteAsync("""
            SELECT coalesce(jsonb_agg(t), '[]'::jsonb)::text
            FROM (
                SELECT *
                FROM membership_applications
                WHERE user_id = @p0 AND NOT is_deleted
                ORDER BY created_at DESC, id
                LIMIT 20 OFFSET @p1
            ) t
            """, ct, userId, (page - 1) * 20));
    });

    [HttpPost]
    public Task<IActionResult> Create(CreateRequest input, CancellationToken ct) => Execute(async () =>
    {
        // Read the owning service, not its database. Snapshot the current company name.
        using var response = await clients.CreateClient("companies").GetAsync($"api/Companies/GetById/{input.CompanyId}", ct);
        if (!response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return BadRequest("Company not found");
        }
        var company = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (company.ValueKind != JsonValueKind.Object)
        {
            return BadRequest("Company not found");
        }
        var name = company.GetProperty("name").GetString()!;
        var sourceVersion = company.GetProperty("version").GetInt64();
        await repository.ExecuteAsync("""
            INSERT INTO company_directory(id, name, source_version)
            VALUES (@p0, @p1, @p2)
            ON CONFLICT(id) DO UPDATE
            SET name = excluded.name,
                source_version = excluded.source_version
            WHERE company_directory.source_version < excluded.source_version
            """, ct, input.CompanyId, name, sourceVersion);
        var id = Guid.NewGuid();
        await repository.ExecuteAsync("CALL create_application(@p0,@p1,@p2,@p3)", ct, id, input.UserId, input.CompanyId, input.Role);
        return CreatedAtAction(nameof(Get), new { id }, new { id });
    });

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, EditRequest input, CancellationToken ct) => Execute(async () =>
    {
        await repository.ExecuteAsync("CALL update_application(@p0,@p1,@p2)", ct, id, input.Role, input.Version);
        return NoContent();
    });

    [HttpPost("{id:guid}/status")]
    public Task<IActionResult> Status(Guid id, StatusRequest input, CancellationToken ct) => Execute(async () =>
    {
        await repository.ExecuteAsync("CALL change_application_status(@p0,@p1,@p2)", ct, id, input.Status, input.Version);
        return NoContent();
    });

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, long version, CancellationToken ct) => Execute(async () =>
    {
        await repository.ExecuteAsync("CALL delete_application(@p0,@p1)", ct, id, version);
        return NoContent();
    });
}
