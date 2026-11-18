namespace CompanyService.Domain.Entities;

public class CompanyDetails
{
    public string Address { get; set; } = "";
    public string? Website
    {
        get; set;
    }
    public int? FoundedYear
    {
        get; set;
    }
}
