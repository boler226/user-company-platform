using Microsoft.AspNetCore.Identity;
using UsersService.Domain.Enums;

namespace UsersService.Domain.Entities;

public class User
{
    public Guid Id
    {
        get; set;
    }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public long Version
    {
        get; set;
    }
    public bool IsDeleted
    {
        get; set;
    }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public Role Role { get; set; } = Role.User;
    public Guid? CompanyId
    {
        get; set;
    }
}
