using CompanyService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanyService.Infrastructure.Configurations;

public sealed class CompanyTagConfiguration : IEntityTypeConfiguration<CompanyTag>
{
    public void Configure(EntityTypeBuilder<CompanyTag> builder)
    {
        builder.ToTable("company_tags");
        builder.HasKey(x => new { x.CompanyId, x.TagId });
        builder.Property(x => x.AddedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.HasOne(x => x.Company)
            .WithMany(x => x.CompanyTags)
            .HasForeignKey(x => x.CompanyId);
        builder.HasOne(x => x.Tag)
            .WithMany(x => x.CompanyTags)
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TagId, x.CompanyId });
        builder.Property(x => x.CompanyId).HasColumnName("company_id");
        builder.Property(x => x.TagId).HasColumnName("tag_id");
        builder.Property(x => x.AddedAt).HasColumnName("added_at");
    }
}
