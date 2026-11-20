namespace UserService.Domain.Memberships;

public class Membership
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyNameSnapshot { get; set; } = "";
    public string RequestedRole { get; set; } = "";
    public string Status { get; set; } = "Submitted";
    public long Version { get; set; }
    public List<MembershipHistory> History { get; set; } = [];
}
