using CompanyService.Domain.Exceptions;

namespace CompanyService.Domain.Entities;

public class CompanyTag
{
    private CompanyTag()
    {
    }

    public CompanyTag(Guid companyId, Guid tagId)
    {
        if (companyId == Guid.Empty || tagId == Guid.Empty)
        {
            throw new DomainException("Company and tag IDs are required.");
        }
        CompanyId = companyId;
        TagId = tagId;
    }

    public Guid CompanyId { get; private set; }
    public Guid TagId { get; private set; }
    public DateTime AddedAt { get; private set; } = DateTime.UtcNow;
    public Company Company { get; private set; } = null!;
    public Tag Tag { get; private set; } = null!;
}
