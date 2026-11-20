using CompanyService.Domain.Exceptions;

namespace CompanyService.Domain.Entities;

public class CompanyDetails
{
    private CompanyDetails()
    {
    }

    public CompanyDetails(Guid companyId, string address)
    {
        if (companyId == Guid.Empty)
        {
            throw new DomainException("Company ID is required.");
        }

        CompanyId = companyId;
        ChangeAddress(address);
    }

    public Guid CompanyId { get; private set; }
    public string Address { get; private set; } = "";
    public string? Website { get; private set; }
    public int? FoundedYear { get; private set; }

    public void ChangeAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address) || address.Length > 300)
        {
            throw new DomainException("Address must contain 1–300 characters.");
        }
        Address = address.Trim();
    }
}
