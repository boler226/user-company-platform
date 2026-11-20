using CompanyService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyService.Infrastructure.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments", table => table.HasCheckConstraint("ck_budget", "budget > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Budget).HasPrecision(18, 2);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CompanyId).HasColumnName("company_id");
        builder.Property(x => x.Name).HasColumnName("name");
        builder.Property(x => x.Budget).HasColumnName("budget");
        builder.Property(x => x.Version).HasColumnName("version");
    }
}
