using System.Collections.ObjectModel;
using MongoDB.Bson;
using NotificationService.Domain.Common;
using NotificationService.Domain.Exceptions;
using NotificationService.Domain.ValueObjects;

namespace NotificationService.Domain.Entities;

public sealed class Notification : BaseEntity
{
    private List<RecentAttempt> _recentAttempts = [];
    private Dictionary<string, string> _payload = [];

    public ObjectId TemplateId { get; private set; }
    public Recipient Recipient { get; private set; } = null!;
    public CompanySnapshot? Company { get; private set; }
    public string Status { get; private set; } = "Pending";
    public int Priority { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public IReadOnlyCollection<RecentAttempt> RecentAttempts => _recentAttempts.AsReadOnly();
    public IReadOnlyDictionary<string, string> Payload => new ReadOnlyDictionary<string, string>(_payload);

    private Notification()
    {
    }

    public Notification(ObjectId templateId, Recipient recipient, CompanySnapshot? company,
        int priority, string kind, string message, DateTime scheduledAt, ObjectId? id = null) : base(id)
    {
        if (templateId == ObjectId.Empty)
            throw new DomainException("Template ID is required.");

        TemplateId = templateId;
        Recipient = recipient ?? throw new DomainException("Recipient is required.");
        Company = company;
        SetContent(priority, kind, message, scheduledAt);
    }

    public void Update(int priority, string kind, string message, DateTime scheduledAt)
    {
        if (IsDeleted || Status != "Pending")
            throw new ConflictException("Only pending notifications can be edited.");

        SetContent(priority, kind, message, scheduledAt);
        Touch();
    }

    public void RecordAttempt(RecentAttempt attempt)
    {
        if (IsDeleted || Status == "Sent")
            throw new ConflictException("This notification cannot receive another delivery attempt.");
        if (_recentAttempts.Any(item => item.AttemptId == attempt.AttemptId))
            throw new ConflictException("This delivery attempt has already been recorded.");

        _recentAttempts.Add(attempt);
        if (_recentAttempts.Count > 3)
            _recentAttempts.RemoveAt(0);

        Status = attempt.Status;
        Touch();
    }

    private void SetContent(int priority, string kind, string message, DateTime scheduledAt)
    {
        if (priority is < 1 or > 5)
            throw new DomainException("Priority must be between 1 and 5.");
        if (string.IsNullOrWhiteSpace(kind) || kind.Length > 100)
            throw new DomainException("Kind must contain 1 to 100 characters.");
        if (string.IsNullOrWhiteSpace(message) || message.Length > 4000)
            throw new DomainException("Message must contain 1 to 4000 characters.");
        if (scheduledAt.Kind != DateTimeKind.Utc)
            throw new DomainException("Scheduled timestamp must be UTC.");

        Priority = priority;
        ScheduledAt = scheduledAt;
        _payload = new Dictionary<string, string>
        {
            ["kind"] = kind.Trim(),
            ["message"] = message.Trim()
        };
    }
}
