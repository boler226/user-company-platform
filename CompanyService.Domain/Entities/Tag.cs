using CompanyService.Domain.Exceptions;

namespace CompanyService.Domain.Entities;

public class Tag
{
    private readonly List<CompanyTag> companyTags = [];
    private Tag()
    {
    }

    public Tag(Guid id, string name)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(name) || name.Length > 50)
        {
            throw new DomainException("A tag ID and name of 1–50 characters are required.");
        }
        Id = id;
        Name = name.Trim();
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public IReadOnlyCollection<CompanyTag> CompanyTags => companyTags;
}
