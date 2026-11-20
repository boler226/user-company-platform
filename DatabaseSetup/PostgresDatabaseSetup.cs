using Npgsql;

using CompanyService.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace DatabaseSetup;

internal sealed class PostgresDatabaseSetup(string adminConnectionString)
{
    public async Task InitializeAsync()
    {
        await using var server = new NpgsqlConnection(adminConnectionString);
        await server.OpenAsync();

        foreach (var context in new[] { "users", "companies" })
        {
            var databaseName = context == "companies" ? "lab03_companies" : "lab01_users";
            var roleName = "lab01_" + context + "_app";
            var password = Environment.GetEnvironmentVariable($"LAB01_{context.ToUpperInvariant()}_PASSWORD")
                ?? $"Lab01{context}Demo2026";

            await CreateDatabaseAsync(server, databaseName);
            await ConfigureLoginAsync(server, databaseName, roleName, password);

            var settings = new NpgsqlConnectionStringBuilder(adminConnectionString)
            {
                Database = databaseName
            };

            await using var connection = new NpgsqlConnection(settings.ConnectionString);
            await connection.OpenAsync();

            if (context == "companies")
            {
                var options = new DbContextOptionsBuilder<CompanyDbContext>()
                    .UseNpgsql(settings.ConnectionString)
                    .Options;
                await using var companyDb = new CompanyDbContext(options);
                var seedDemoData = !string.Equals(
                    Environment.GetEnvironmentVariable("SEED_DEMO_DATA"), "false",
                    StringComparison.OrdinalIgnoreCase);
                await DbInitializer.InitializeAsync(companyDb, seedDemoData);
            }
            else
            {
                await ApplyScriptsAsync(connection, context);
            }
            await GrantPermissionsAsync(connection, context, roleName);

            Console.WriteLine($"Ready: {databaseName}");
        }
    }

    private static async Task CreateDatabaseAsync(NpgsqlConnection server, string databaseName)
    {
        await using var query = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = @name", server);
        query.Parameters.AddWithValue("name", databaseName);

        if (await query.ExecuteScalarAsync() is null)
        {
            await ExecuteAsync(server, $"CREATE DATABASE {databaseName}");
        }
    }

    private static async Task ConfigureLoginAsync(
        NpgsqlConnection server, string databaseName, string roleName, string password)
    {
        await using var query = new NpgsqlCommand(
            "SELECT 1 FROM pg_roles WHERE rolname = @name", server);
        query.Parameters.AddWithValue("name", roleName);

        var operation = await query.ExecuteScalarAsync() is null ? "CREATE" : "ALTER";
        var escapedPassword = password.Replace("'", "''");

        await ExecuteAsync(server, $"{operation} ROLE {roleName} LOGIN PASSWORD '{escapedPassword}'");
        await ExecuteAsync(server, $"""
            REVOKE ALL ON DATABASE {databaseName} FROM PUBLIC;
            GRANT CONNECT ON DATABASE {databaseName} TO {roleName};
            """);
    }

    private static async Task ApplyScriptsAsync(NpgsqlConnection connection, string context)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "scripts", context);
        var scripts = Directory.GetFiles(directory, "*.sql").Order();

        foreach (var path in scripts)
        {
            // Verification scripts are run manually, not during deployment.
            if (Path.GetFileName(path).StartsWith("04-"))
            {
                continue;
            }

            await ExecuteAsync(connection, await File.ReadAllTextAsync(path));
        }
    }

    private static async Task GrantPermissionsAsync(
        NpgsqlConnection connection, string context, string roleName)
    {
        var permissions = context == "users"
            ? $"""
                GRANT SELECT ON ALL TABLES IN SCHEMA public TO {roleName};
                GRANT INSERT, UPDATE ON users, user_profiles, company_directory TO {roleName};
                GRANT EXECUTE ON ALL PROCEDURES IN SCHEMA public TO {roleName};
                """
            : $"GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO {roleName};";

        await ExecuteAsync(connection, $"""
            REVOKE ALL ON SCHEMA public FROM PUBLIC;
            GRANT USAGE ON SCHEMA public TO {roleName};
            REVOKE ALL ON ALL TABLES IN SCHEMA public FROM {roleName};
            REVOKE ALL ON ALL ROUTINES IN SCHEMA public FROM PUBLIC;
            {permissions}
            """);
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
