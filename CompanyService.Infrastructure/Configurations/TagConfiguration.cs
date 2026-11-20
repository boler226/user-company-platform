using CompanyService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyService.Infrastructure.Configurations;

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.Navigation(x => x.CompanyTags).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasData(
            new Tag(Guid.Parse("50000000-0000-0000-0000-000000000001"), "IT"),
            new Tag(Guid.Parse("50000000-0000-0000-0000-000000000002"), "Remote"));
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name");
    }
}
