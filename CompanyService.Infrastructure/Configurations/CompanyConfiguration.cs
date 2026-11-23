using CompanyService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyService.Infrastructure.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies", table => table.HasCheckConstraint("ck_company_name", "length(trim(name)) > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(254).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.Name);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Ignore(x => x.Address);
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasOne(x => x.Details)
            .WithOne()
            .HasForeignKey<CompanyDetails>(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Departments)
            .WithOne()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(x => x.Departments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.CompanyTags).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name");
        builder.Property(x => x.Email).HasColumnName("email");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.Version).HasColumnName("version");
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted");
    }
}
