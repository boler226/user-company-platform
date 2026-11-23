using MongoDB.Bson;
using NotificationService.Domain.Common;
using NotificationService.Domain.Exceptions;

namespace NotificationService.Domain.ValueObjects;

public sealed class RecentAttempt : ValueObject
{
    public ObjectId AttemptId { get; }
    public string Status { get; }
    public DateTime OccurredAt { get; }

    public RecentAttempt(ObjectId attemptId, string status, DateTime occurredAt)
    {
        if (attemptId == ObjectId.Empty)
            throw new DomainException("Attempt ID is required.");
        if (status is not ("Sent" or "Failed"))
            throw new DomainException("Attempt status must be Sent or Failed.");
        if (occurredAt.Kind != DateTimeKind.Utc)
            throw new DomainException("Attempt timestamp must be UTC.");

        AttemptId = attemptId;
        Status = status;
        OccurredAt = occurredAt;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AttemptId;
        yield return Status;
        yield return OccurredAt;
    }
}
