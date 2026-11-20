namespace UserService.Domain.Memberships;

public class MembershipHistory
{
    public Guid Id { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
