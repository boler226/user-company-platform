using MongoDB.Bson;
using NotificationService.Domain.Exceptions;

namespace NotificationService.Domain.Common;

public abstract class BaseEntity
{
    public ObjectId Id { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public long Version { get; private set; } = 1;
    public string CreatedBy { get; private set; } = "notifications-api";
    public string UpdatedBy { get; private set; } = "notifications-api";
    public bool IsDeleted { get; private set; }

    protected BaseEntity(ObjectId? id = null)
    {
        Id = id ?? ObjectId.GenerateNewId();
        if (Id == ObjectId.Empty)
            throw new DomainException("Entity ID cannot be empty.");
    }

    protected void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
        Version = checked(Version + 1);
    }

    public void Delete()
    {
        IsDeleted = true;
        Touch();
    }
}
