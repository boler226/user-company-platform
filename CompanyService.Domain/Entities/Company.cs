using CompanyService.Domain.Exceptions;

namespace CompanyService.Domain.Entities;

public class Company
{
    private readonly List<Department> departments = [];
    private readonly List<CompanyTag> companyTags = [];

    private Company()
    {
    }

    public Company(string name, string email, string address, Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        if (Id == Guid.Empty)
        {
            throw new DomainException("Company ID is required.");
        }

        Details = new CompanyDetails(Id, address);
        Rename(name, email, address);
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Email { get; private set; } = "";
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public long Version { get; private set; }
    public bool IsDeleted { get; private set; }
    public CompanyDetails Details { get; private set; } = null!;
    public string Address => Details.Address;
    public IReadOnlyCollection<Department> Departments => departments;
    public IReadOnlyCollection<CompanyTag> CompanyTags => companyTags;

    public void Rename(string name, string email, string address)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
        {
            throw new DomainException("Company name must contain 1–100 characters.");
        }
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254 || !email.Contains('@'))
        {
            throw new DomainException("A valid company email is required.");
        }

        Details.ChangeAddress(address);
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Version++;
    }

    public void Archive()
    {
        IsDeleted = true;
        Version++;
    }
}
