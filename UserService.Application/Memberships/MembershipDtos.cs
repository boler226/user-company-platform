using System.ComponentModel.DataAnnotations;

namespace UserService.Application.Memberships;

public record CreateMembershipRequest(
    Guid UserId,
    Guid CompanyId,
    [Required, StringLength(50, MinimumLength = 1)] string Role);

public record EditMembershipRequest(
    [Required, StringLength(50, MinimumLength = 1)] string Role,
    [Range(1, long.MaxValue)] long Version);

public record ChangeMembershipStatusRequest(
    [Required] string Status,
    [Range(1, long.MaxValue)] long Version);

public class MembershipDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyNameSnapshot { get; set; } = "";
    public string RequestedRole { get; set; } = "";
    public string Status { get; set; } = "";
    public long Version { get; set; }
    public List<MembershipHistoryDto> History { get; set; } = [];
}

public class MembershipHistoryDto
{
    public Guid Id { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
