using CompanyService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyService.Infrastructure.Configurations;

public sealed class CompanyDetailsConfiguration : IEntityTypeConfiguration<CompanyDetails>
{
    public void Configure(EntityTypeBuilder<CompanyDetails> builder)
    {
        builder.ToTable("company_details");
        builder.HasKey(x => x.CompanyId);
        builder.Property(x => x.Address).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Website).HasMaxLength(300);
        builder.ToTable(t => t.HasCheckConstraint("ck_founded_year", "founded_year BETWEEN 1800 AND 2100"));
        builder.Property(x => x.CompanyId).HasColumnName("company_id");
        builder.Property(x => x.Address).HasColumnName("address");
        builder.Property(x => x.Website).HasColumnName("website");
        builder.Property(x => x.FoundedYear).HasColumnName("founded_year");
    }
}
