using Microsoft.EntityFrameworkCore;
using UsersService.Domain.Entities;

namespace UsersService.Infrastructure.DbContext;

public class UserDbContext(DbContextOptions<UserDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<User>();
        e.ToTable("users");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.Email).HasColumnName("email");
        e.Property(x => x.FirstName).HasColumnName("first_name");
        e.Property(x => x.LastName).HasColumnName("last_name");
        e.Property(x => x.PasswordHash).HasColumnName("password_hash");
        e.Property(x => x.Role).HasColumnName("role");
        e.Property(x => x.CompanyId).HasColumnName("company_id");
        e.Property(x => x.RegisteredAt).HasColumnName("created_at");
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate();
        e.HasQueryFilter(x => !x.IsDeleted);
    }
}
