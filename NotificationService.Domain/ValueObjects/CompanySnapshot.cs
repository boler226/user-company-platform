using NotificationService.Domain.Common;
using NotificationService.Domain.Exceptions;

namespace NotificationService.Domain.ValueObjects;

public sealed class CompanySnapshot : ValueObject
{
    public string Id { get; }
    public string Name { get; }

    public CompanySnapshot(string id, string name)
    {
        if (!Guid.TryParse(id, out var companyId) || companyId == Guid.Empty)
            throw new DomainException("Company ID must be a non-empty UUID.");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            throw new DomainException("Company name must contain 1 to 200 characters.");

        Id = companyId.ToString();
        Name = name.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
        yield return Name;
    }
}
