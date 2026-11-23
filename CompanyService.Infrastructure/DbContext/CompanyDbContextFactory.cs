using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CompanyService.Infrastructure.DbContext;

public sealed class CompanyDbContextFactory : IDesignTimeDbContextFactory<CompanyDbContext>
{
    public CompanyDbContext CreateDbContext(string[] args)
    {
        // Scaffolding migrations only needs the provider and model, not a running database.
        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_ADMIN")
            ?? "Host=localhost;Database=lab03_companies;Username=postgres";
        var options = new DbContextOptionsBuilder<CompanyDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new CompanyDbContext(options);
    }
}
