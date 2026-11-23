using Microsoft.EntityFrameworkCore;
using CompanyService.Domain.Entities;

namespace CompanyService.Infrastructure.DbContext;

public class CompanyDbContext(DbContextOptions<CompanyDbContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<CompanyTag> CompanyTags => Set<CompanyTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CompanyDbContext).Assembly);
}
