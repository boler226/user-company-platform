using System.Net.Mail;
using NotificationService.Domain.Common;
using NotificationService.Domain.Exceptions;

namespace NotificationService.Domain.ValueObjects;

public sealed class Recipient : ValueObject
{
    public string UserId { get; }
    public string Email { get; }
    public string DisplayName { get; }

    public Recipient(string userId, string email, string displayName)
    {
        if (!Guid.TryParse(userId, out var id) || id == Guid.Empty)
            throw new DomainException("Recipient user ID must be a non-empty UUID.");
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254
            || !MailAddress.TryCreate(email, out var address) || address.Address != email)
            throw new DomainException("A valid email address is required.");
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 200)
            throw new DomainException("Display name must contain 1 to 200 characters.");

        UserId = id.ToString();
        Email = email;
        DisplayName = displayName.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserId;
        yield return Email;
        yield return DisplayName;
    }
}
