using Npgsql;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace UserService.Infrastructure.Repositories;

public class MembershipRepository(IConfiguration configuration)
{
    public async Task<JsonElement?> ExecuteAsync(string statement, CancellationToken ct, params object[] values)
    {
        await using var connection = new NpgsqlConnection(configuration.GetConnectionString("PostgreSQLConnection"));
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand(statement, connection);
        for (var i = 0; i < values.Length; i++)
            command.Parameters.AddWithValue("p" + i, values[i]);
        var result = await command.ExecuteScalarAsync(ct);
        return result is string json ? JsonSerializer.Deserialize<JsonElement>(json) : null;
    }
}
