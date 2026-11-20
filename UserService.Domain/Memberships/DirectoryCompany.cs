namespace UserService.Domain.Memberships;

public class DirectoryCompany
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public long SourceVersion { get; set; }
    public long Version { get; set; }
}
