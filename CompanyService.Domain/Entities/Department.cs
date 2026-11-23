using CompanyService.Domain.Exceptions;

namespace CompanyService.Domain.Entities;

public class Department
{
    private Department()
    {
    }

    public Department(Guid companyId, string name, decimal budget, Guid? id = null)
    {
        if (companyId == Guid.Empty || string.IsNullOrWhiteSpace(name) || name.Length > 100)
        {
            throw new DomainException("A company and department name are required.");
        }
        Id = id ?? Guid.NewGuid();
        if (Id == Guid.Empty)
        {
            throw new DomainException("Department ID is required.");
        }

        CompanyId = companyId;
        Name = name.Trim();
        ChangeBudget(budget);
    }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = "";
    public decimal Budget { get; private set; }
    public long Version { get; private set; }

    public void ChangeBudget(decimal budget)
    {
        if (budget <= 0 || budget > 9999999999999999.99m || decimal.Round(budget, 2) != budget)
        {
            throw new DomainException("Budget must be positive and have at most two decimal places.");
        }
        Budget = budget;
        Version++;
    }
}
