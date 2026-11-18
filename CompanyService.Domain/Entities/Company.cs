using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompanyService.Domain.Entities;

public class Company
{
    public Guid Id
    {
        get; set;
    }
    public string Name { get; set; } = null!;
    public CompanyDetails Details { get; set; } = new();
    public string Address
    {
        get => Details.Address; set => Details.Address = value;
    }
    public long Version
    {
        get; set;
    }
    public bool IsDeleted
    {
        get; set;
    }
    public string Email { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
