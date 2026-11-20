using System.Data;
using Npgsql;
using UserService.Domain.Memberships;

namespace UserService.Infrastructure.Memberships;

public abstract class RepositoryBase(
    NpgsqlConnection connection,
    Func<NpgsqlTransaction?> currentTransaction)
{
    protected NpgsqlConnection Connection => connection;
    protected NpgsqlTransaction? Transaction => currentTransaction();
    protected const int CommandTimeout = 30;

    protected async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct)
    {
        try
        {
            if (Connection.State != ConnectionState.Open)
            {
                await Connection.OpenAsync(ct);
            }

            return await operation();
        }
        catch (PostgresException exception)
        {
            // Database messages can contain SQL or sensitive values; expose only domain messages.
            throw exception.SqlState switch
            {
                "P0002" => new ResourceNotFoundException("The requested record was not found."),
                "P0001" => new BusinessConflictException("This operation is not allowed in the current state."),
                "23505" => new BusinessConflictException("The record already exists."),
                "40001" or "40P01" => new BusinessConflictException("The data changed concurrently. Reload and retry."),
                "23503" => new BusinessConflictException("A related record is missing or is still in use."),
                "23514" or "23502" or "22001" => new InputValidationException("The supplied values violate data constraints."),
                _ => exception
            };
        }
    }
}
