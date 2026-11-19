using Microsoft.EntityFrameworkCore;
using CompanyService.Domain.Entities;

namespace CompanyService.Infrastructure.DbContext;

public class CompanyDbContext(DbContextOptions<CompanyDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<CompanyTag> CompanyTags => Set<CompanyTag>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var company = modelBuilder.Entity<Company>();
        company.ToTable("companies");
        company.HasKey(x => x.Id);
        company.Ignore(x => x.Address);
        company.Property(x => x.Id).HasColumnName("id");
        company.Property(x => x.Name).HasColumnName("name");
        company.Property(x => x.Email).HasColumnName("email");
        company.Property(x => x.CreatedAt).HasColumnName("created_at");
        company.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        company.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();
        company.HasQueryFilter(x => !x.IsDeleted);
        company.OwnsOne(x => x.Details, details =>
        {
            details.ToTable("company_details");
            details.WithOwner().HasForeignKey("CompanyId");
            details.Property<Guid>("CompanyId").HasColumnName("company_id");
            details.Property(x => x.Address).HasColumnName("address");
            details.Property(x => x.Website).HasColumnName("website");
            details.Property(x => x.FoundedYear).HasColumnName("founded_year");
        });

        var department = modelBuilder.Entity<Department>();
        department.ToTable("departments");
        department.HasKey(x => x.Id);
        department.Property(x => x.Id).HasColumnName("id");
        department.Property(x => x.CompanyId).HasColumnName("company_id");
        department.Property(x => x.Name).HasColumnName("name");
        department.Property(x => x.Budget).HasColumnName("budget");
        department.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();
        department.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);

        var tag = modelBuilder.Entity<Tag>();
        tag.ToTable("tags");
        tag.HasKey(x => x.Id);
        tag.Property(x => x.Id).HasColumnName("id");
        tag.Property(x => x.Name).HasColumnName("name");

        var link = modelBuilder.Entity<CompanyTag>();
        link.ToTable("company_tags");
        link.HasKey(x => new { x.CompanyId, x.TagId });
        link.Property(x => x.CompanyId).HasColumnName("company_id");
        link.Property(x => x.TagId).HasColumnName("tag_id");
        link.Property(x => x.AddedAt).HasColumnName("added_at");
        link.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId);
        link.HasOne<Tag>().WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
    }
}
