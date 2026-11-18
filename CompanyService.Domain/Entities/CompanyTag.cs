namespace CompanyService.Domain.Entities;

public class CompanyTag
{
    public Guid CompanyId
    {
        get; set;
    }
    public Guid TagId
    {
        get; set;
    }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
